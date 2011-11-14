using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using RemoteDependencies;

namespace RevitRemoteBoot
{
    class Program
    {
        static void Main(string[] args)
        {
            //check the arguments
            if (args.Length < 4)
            {
                Console.WriteLine("Need to input Revit Server Path " + 
                    " + Export File Location + DLL + Class Name");
                return;
            }

            //read parameters
            string revitServerPath = args[0];
            string revitFile = args[1];
            string revitDir = Path.GetDirectoryName(revitFile);
            string dllFile = args[2];
            string className = args[3];
            Guid uniqueId = Guid.NewGuid();

            //drop zone location - should be kept somewhere else
            string dropZoneLocation = @"C:\RRBTest\";


            RevitQueueItem item = new RevitQueueItem(revitFile, dllFile, className,
                                                     uniqueId, dropZoneLocation);

            //read any custom parameters that may have been added
            for (int i = 3; i < args.Length; i++)
            {
                string customArgument = args[i];
                item.AddArgument(customArgument);
            }

            item.SaveToDropZone();

            string revitToolLocation =
                @"C:\Program Files\Autodesk\Revit Structure 2012\Program\RevitServerToolCommand\RevitServerTool.exe";

            string arguments = "createLocalRVT " + revitServerPath + " -d " + revitFile + " -s localhost -o";
                

            Console.WriteLine("Running RevitServerToolCommand with arguments " + arguments);
            //create local file if its on a revit server using the revit server command line tool
            Process revitServerProcess = new Process();
            revitServerProcess.StartInfo = new ProcessStartInfo();
            revitServerProcess.StartInfo.UseShellExecute = true;
            revitServerProcess.StartInfo.WorkingDirectory = revitDir;
            revitServerProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            revitServerProcess.StartInfo.FileName = revitToolLocation;
            revitServerProcess.StartInfo.Arguments = arguments;
            revitServerProcess.Start();


            //wait no longer than a minute for it to create a local file
            //if it takes too long, cancel it 
            if (!revitServerProcess.WaitForExit(60 * 1000))
            {
                Console.WriteLine("Local file creation failed");
                return;
            } else
            {
                  Console.WriteLine("Created local file at " + revitFile);
            }
            
            //start Revit and open the file
            Process p = new Process();
            p.StartInfo = new ProcessStartInfo();
            p.StartInfo.UseShellExecute = true;
            p.StartInfo.WorkingDirectory = revitDir;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            p.StartInfo.FileName = revitFile;
            p.Start();

            //wait for command to run and marked complete
            int loopCount = 0;
            //max of 500 seconds
            while (loopCount < 100 && !item.CheckComplete())
            {
                //sleep 5 seconds
                Thread.Sleep(5000);
            }
            p.Kill();
            //close revit
            p.Close();


        }
    }
}
