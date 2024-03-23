using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RevitExtensions
{
    [Transaction(TransactionMode.Manual)]
    public class CreateSectionsView : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Run application
                // Get the handle of current document.
                Document doc = commandData.Application.ActiveUIDocument.Document;
                Transaction transaction = new Transaction(doc, "Modify Columns");

                transaction.Start();

                TaskDialog.Show("Inicio de Crear Vistas", "Solo una prueba");
                // 💻 Get all elements (columns) instances of each type.

                IList<Element> columns = new FilteredElementCollector(doc).
                    OfCategory(BuiltInCategory.OST_StructuralColumns).
                    WhereElementIsNotElementType().
                    ToElements();
                // TODO: redefine types in dict.
                Dictionary<string, Element> dict_columns = new Dictionary<string, Element>();

                foreach (var column in columns)
                {
                    ElementId elementId = column.GetTypeId();
                    ElementType type = doc.GetElement(elementId) as ElementType;

                    String familiy_name = type.FamilyName;
                    String type_name = type.Name;
                    String key_name = $"{familiy_name}_{type_name}";

                    // Only last element with stay in the dict.
                    dict_columns[key_name] = column;
                }

                // 💻 Create sections
                foreach (KeyValuePair<string, Element> columnTuple in dict_columns)
                {
                    // Get origin of element
                    XYZ column_origin = (columnTuple.Value.Location as LocationPoint).Point;

                    // Calculate vector based in host
                    // In case of column use FacingOrientation
                    XYZ faceOrientation = (columnTuple.Value as FamilyInstance).FacingOrientation;
                    XYZ vector = faceOrientation - column_origin;
                    // Get element size
                    ElementId elementId = columnTuple.Value.GetTypeId();
                    ElementType type = doc.GetElement(elementId) as ElementType;
                    // Width based in parameter
                    double elementWidth = type.GetParameters("b").First().AsDouble();

                    BoundingBoxXYZ boundingBox = columnTuple.Value.get_BoundingBox(null);
                    // Height based in bounding box
                    double elementHeight = boundingBox.Max.Z - boundingBox.Min.Z;
                    // Video en 9:22
                }
                return Autodesk.Revit.UI.Result.Succeeded;

            }
            catch (Exception e)
            {
                message = e.Message;
                TaskDialog.Show("Error en Execute", $"{e.Message} \n {e.StackTrace}");
                return Autodesk.Revit.UI.Result.Failed;
            }
        }
    }
}
