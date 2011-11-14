using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Serialization;

namespace RemoteDependencies
{
    /// <summary>
    /// Represents a class that will hold a queue item fo rrevit ro  read 
    /// when it is initialized by the remote boot sequence. 
    /// </summary>
    [Serializable()]
    public class RevitQueueItem 
    {
        /// <summary>
        /// Location of the revit file to perform the operations on 
        /// </summary>
        public string RevitFileLocation { get; set; }

        /// <summary>
        /// Locations of the dll that contains the operation to preform 
        /// </summary>
        public string DllLocation { get; set; }

        /// <summary>
        /// Class name from the dll file that contains the code to run
        /// Must implement IRemoteCommand
        /// </summary>
        public string ClassName { get; set; }

        /// <summary>
        /// A list of extra custom arguments to present to revit
        /// may be used in running the actual tool itself 
        /// </summary>
        public List<string> CustomArgs { get; set; }
        
        /// <summary>
        /// The location of the dropzone, should be constant generally
        /// </summary>
        public string DropZoneLocation { get; set; }
        
        /// <summary>
        /// A unique identifier for the queue item so that multiple instances of the same 
        /// command may be stored in the dropzone.
        /// </summary>
        public Guid Identifier { get; set; }

        /// <summary>
        /// Stores the full path the the queue item
        /// </summary>
        public string QueueItemPath { get; set; }

        public RevitQueueItem()
        {
            
        }
        public RevitQueueItem(string revitFileLocation, string dllLocation, 
            string className, Guid identifier, string dropZoneLocation)
        {
            RevitFileLocation = revitFileLocation;
            DllLocation = dllLocation;
            ClassName = className;
            Identifier = identifier;
            DropZoneLocation = dropZoneLocation;
            CustomArgs = new List<string>();
            QueueItemPath = Path.Combine(DropZoneLocation, Identifier + ".rrb");
        }

        /// <summary>
        /// Adds a custom argument to the item
        /// </summary>
        /// <param name="argument"></param>
        public void AddArgument(string argument)
        {
            CustomArgs.Add(argument);
        }

        /// <summary>
        /// Serializes the class into the drop zone, names it by the guid.
        /// </summary>
        public void SaveToDropZone()
        {
            XmlSerializer xmlSerializer = new XmlSerializer(this.GetType());
            StreamWriter writer = new StreamWriter(this.QueueItemPath, false);
            xmlSerializer.Serialize(writer, this);
            writer.Close();
        }


        /// <summary>
        /// Marks the item as complete
        /// </summary>
        public void MarkComplete()
        {
            File.Delete(this.QueueItemPath);
            
        }

        /// <summary>
        /// Checks if the action has been completed yet or not.
        /// </summary>
        /// <returns></returns>
        public bool CheckComplete()
        {
            return !File.Exists(this.QueueItemPath);
        }

        /// <summary>
        /// Checks if the queue item is intended for a certain file.
        /// </summary>
        /// <param name="revitFile"></param>
        /// <returns></returns>
        public bool IsValidForFile(string revitFile)
        {
            return revitFile.ToLower().Equals(this.RevitFileLocation.ToLower());

        }

        /// <summary>
        /// Deserializes the queue items from the specified drop zone.
        /// </summary>
        /// <param name="dropZoneLocation">drop zone</param>
        /// <returns></returns>
        public static IList<RevitQueueItem> ReadItemsFromDropZone(string dropZoneLocation)
        {
            IList<RevitQueueItem> items = new List<RevitQueueItem>();

            //TODO: Make it properly check the file format before loading in.
            foreach (string fileName in Directory.GetFiles(dropZoneLocation, "*.rrb"))
            {
                XmlSerializer serializer = new XmlSerializer(typeof (RevitQueueItem));
                StreamReader reader = new StreamReader(fileName);
                RevitQueueItem item = serializer.Deserialize(reader) as RevitQueueItem;
                items.Add(item);
                reader.Close();
            }
            return items;
        }

    }
}
