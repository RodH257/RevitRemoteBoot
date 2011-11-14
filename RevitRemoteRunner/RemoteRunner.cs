using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using RemoteDependencies;

namespace RevitRemoteRunner
{
    /// <summary>
    /// Processes a queue that is present in XML files in a dropzone
    /// Reads these files and looks for the DLl they reference
    /// casts those DLL's to IRemoteCommand and runs them. 
    /// Marks complete when finished.
    /// </summary>
    [Regeneration(RegenerationOption.Manual)]
    [Transaction(TransactionMode.Manual)]
    public class RemoteRunner : IExternalApplication
    {
        /// <summary>
        /// Register the events on startup
        /// </summary>
        /// <param name="application"></param>
        /// <returns></returns>
        public Result OnStartup(UIControlledApplication application)
        {
            application.ControlledApplication.DocumentOpened += new EventHandler<Autodesk.Revit.DB.Events.DocumentOpenedEventArgs>(ControlledApplication_DocumentOpened);
            return Result.Succeeded;
        }

        /// <summary>
        /// When the document is opened, cehck the dropzone and process the queue.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ControlledApplication_DocumentOpened(object sender, Autodesk.Revit.DB.Events.DocumentOpenedEventArgs e)
        {
            //check for files in drop zone accessor dir
            
            string dropZoneDir = @"C:\RRBTest\";

            try
            {
                //check for queue files to run
                foreach (RevitQueueItem item in RevitQueueItem.ReadItemsFromDropZone(dropZoneDir))
                {
                    if (item.IsValidForFile(e.Document.PathName))
                    {
                        ProcessQueueItem(item, e.Document);
                    }
                }
            } catch (Exception ex)
            {
                TaskDialog.Show("Error", ex.ToString());
            }
        }

        /// <summary>
        /// Performs the actions on a certain queue item.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="doc"></param>
        private void ProcessQueueItem(RevitQueueItem item, Document doc)
        {
            string dllLocation = item.DllLocation;
            string className = item.ClassName;
            //foreach of those.
            //load the dll with reflection
            Type type = LoadDllWithReflection(dllLocation, className);
            IRemoteCommand command = GetCommandFromType(type);

            //Check for transaction attribute, start one if automatic
            Transaction transaction = StartAutomaticTransaction(type, doc);
            
            //run the execute method
            Result result = command.RunRemotely(doc);

            //close the transaction 
            if (transaction != null && result == Result.Succeeded)
                transaction.Commit();

            if (transaction != null && (result == Result.Cancelled || result == Result.Failed))
                transaction.RollBack();
         
            item.MarkComplete();
         
        }

        /// <summary>
        /// Checks for and starts automatic transaction
        /// </summary>
        /// <param name="type"></param>
        /// <param name="doc"></param>
        /// <returns></returns>
        private Transaction StartAutomaticTransaction(Type type, Document doc)
        {
            //check for transactionmode attribute, if automatic, start one 
            Attribute[] attrs = Attribute.GetCustomAttributes(type);

            Transaction transaction = null;
            //start transaction
            foreach (Attribute attr in attrs)
            {
                if (attr is TransactionAttribute)
                {
                    TransactionAttribute transAt = (TransactionAttribute)attr;
                    if (transAt.Mode == TransactionMode.Automatic)
                    {
                        //needs automatic transaction mode.
                        transaction = new Transaction(doc);
                        transaction.Start("Remote Runner");
                    }
                }
            }
            return transaction;
        }

        /// <summary>
        /// Uses reflection to load the dll
        /// </summary>
        /// <param name="dllLocation"></param>
        /// <param name="className"></param>
        /// <returns></returns>
        private Type LoadDllWithReflection(string dllLocation, string className)
        {
            Assembly assembly = Assembly.LoadFrom(dllLocation);
            Type type = assembly.GetType(className);
            try
            {
                foreach (Type t in assembly.GetTypes())
                {
                    if (t.FullName.EndsWith(className))
                        type = t;
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                var exceptions = ex.LoaderExceptions;
                foreach (var failedType in ex.Types)
                {
                    if (failedType != null)
                        Debug.WriteLine(failedType.FullName);
                }
                foreach (Exception loadException in exceptions)
                {
                    Debug.WriteLine(loadException.ToString());
                }
            }
            return type;
        }

        /// <summary>
        /// Gets the command from type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private IRemoteCommand GetCommandFromType(Type type)
        {
            IRemoteCommand command = Activator.CreateInstance(type) as IRemoteCommand;
            if (command == null)
                throw new Exception("Could not get Remote Command");

            return command;
        }

        /// <summary>
        /// Clean up on shut down
        /// </summary>
        /// <param name="application"></param>
        /// <returns></returns>
        public Result OnShutdown(UIControlledApplication application)
        {
            application.ControlledApplication.DocumentOpened -= ControlledApplication_DocumentOpened;
            return Result.Succeeded;
        }
    }
}
