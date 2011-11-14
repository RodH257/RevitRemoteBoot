using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitRemoteRunner;

namespace ScheduledExport
{
    [Regeneration(RegenerationOption.Manual)]
    [Transaction(TransactionMode.Automatic)]
    public class Command : IExternalCommand, IRemoteCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return RunRemotely(commandData.Application.ActiveUIDocument.Document);
        }

        public Result RunRemotely(Document document)
        {
            FilteredElementCollector collector = new FilteredElementCollector(document);
            collector.OfClass(typeof(ViewSheet));
           
            ViewSet set = new ViewSet();
            foreach (View view in collector)
            {
                set.Insert(view);
            }
            DWFXExportOptions options = new DWFXExportOptions();
            options.MergedViews = true;
            options.ImageQuality = DWFImageQuality.High;
            options.ExportObjectData = true;

            document.Export(@"C:\RRBTest\", "TestExport.dwfx", set, options);
            return Result.Succeeded;
        }
    }
}
