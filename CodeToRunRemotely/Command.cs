using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitRemoteRunner;

namespace CodeToRunRemotely
{
    public class Command : IExternalCommand, IRemoteCommand 
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
           return RunRemotely(commandData.Application.ActiveUIDocument.Document);
        }

        public Result RunRemotely(Document document)
        {
            FilteredElementCollector collector = new FilteredElementCollector(document);
            ElementCategoryFilter filter = new ElementCategoryFilter(BuiltInCategory.OST_StructuralColumns);
            TextWriter writer = new StreamWriter(@"C:\ColumnOutput.txt", true);
            writer.WriteLine("Column IDs");

            foreach (Element e in collector.WherePasses(filter))
            {
                writer.WriteLine(e.Name + " " + e.Id);
            }
            writer.WriteLine("Written: " + DateTime.Now);
            writer.Close();
            return Result.Succeeded;
        }
    }
}
