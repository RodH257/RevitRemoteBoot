using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitRemoteRunner
{
   public interface IRemoteCommand
    {
         Result RunRemotely(Document document);
    }
}
