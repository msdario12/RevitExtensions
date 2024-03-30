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
            // Get the handle of current document.
            Document doc = commandData.Application.ActiveUIDocument.Document;
            Transaction transaction = new Transaction(doc, "Create new section view");
            try
            {
                // Run application
                TaskDialog.Show("Inicio de Crear Vistas REVIT", "Usando Ribbon de Revit");
                // Get filtered elements by category
                Dictionary<string, Element> dict_columns = getElementsBasedInCategory(doc, transaction);

                // 💻 Create sections
                foreach (KeyValuePair<string, Element> columnTuple in dict_columns)
                {
                    // Get origin of element
                    XYZ column_origin = (columnTuple.Value.Location as LocationPoint).Point;

                    // Calculate vector based in host
                    // In case of column use FacingOrientation
                    XYZ faceOrientation = (columnTuple.Value as FamilyInstance).FacingOrientation;
                    XYZ vector = faceOrientation;
                    // Get element size
                    ElementId elementId = columnTuple.Value.GetTypeId();
                    ElementType type = doc.GetElement(elementId) as ElementType;

                    BoundingBoxXYZ boundingBox = columnTuple.Value.get_BoundingBox(null);
                    // Width based in parameter
                    //double elementWidth = type.GetParameters("b").First().AsDouble();
                    double elementWidth = boundingBox.Max.X - boundingBox.Min.X;
                    // Height based in bounding box
                    double elementHeight = boundingBox.Max.Z - boundingBox.Min.Z;
                    // Video en 9:22
                    double elementDepth = UnitUtils.ConvertToInternalUnits(40, UnitTypeId.Centimeters);
                    double offset = UnitUtils.ConvertToInternalUnits(40, UnitTypeId.Centimeters);

                    //4 Create Transform (Origin Point + X,Y,Z vectors)
                    Transform transformIdentity = Transform.Identity;
                    transformIdentity.Origin = column_origin;

                    // Normalize vectors
                    vector = vector.Normalize();

                    transformIdentity.BasisX = vector;
                    transformIdentity.BasisY = XYZ.BasisZ;
                    transformIdentity.BasisZ = vector.CrossProduct(XYZ.BasisZ);

                    // Create new boundary box
                    BoundingBoxXYZ sectionBox = new BoundingBoxXYZ();
                    double half = elementWidth / 2;

                    sectionBox.Min = new XYZ(-half - offset, 0 - offset, -elementDepth);
                    sectionBox.Max = new XYZ(+half + offset, elementHeight + offset, elementDepth);
                    // Apply transforms to section box
                    sectionBox.Transform = transformIdentity;

                    //6 Create sections view.
                    ElementId sectionTypeId = doc.GetDefaultElementTypeId(ElementTypeGroup.ViewTypeSection);
                    ViewSection sectionView = ViewSection.CreateSection(doc, sectionTypeId, sectionBox);
                    // Fine detail level
                    sectionView.DetailLevel = ViewDetailLevel.Fine;
                    sectionView.Scale = 50;

                    string viewName = $"API_{columnTuple.Key}";
                    for (int i = 0; i < 10; i++)
                    {
                        try
                        {
                            sectionView.Name = viewName;
                            break;
                        }
                        catch (Exception)
                        {
                            viewName += "*";
                        }
                    }
                }
                transaction.Commit();
                return Autodesk.Revit.UI.Result.Succeeded;

            }
            catch (Exception e)
            {
                message = e.Message;
                TaskDialog.Show("Error en Execute", $"{e.Message} \n {e.StackTrace}");
                transaction.RollBack();
                return Autodesk.Revit.UI.Result.Failed;
            }
        }

        private static Dictionary<string, Element> getElementsBasedInCategory(Document doc, Transaction transaction)
        {
            // 💻 Get all elements (columns) instances of each type.
            IList<Element> columns = new FilteredElementCollector(doc).
                OfCategory(BuiltInCategory.OST_StructuralColumns).
                WhereElementIsNotElementType().
                ToElements();

            Dictionary<string, Element> dict_columns = new Dictionary<string, Element>();

            transaction.Start();

            foreach (var column in columns)
            {
                ElementId elementId = column.GetTypeId();
                ElementType type = doc.GetElement(elementId) as ElementType;

                String numeration = column.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).AsString();
                String familiy_name = type.FamilyName;
                String type_name = type.Name;
                String key_name = $"{numeration}_{familiy_name}_{type_name}";

                // Only last element with stay in the dict.
                //dict_columns[key_name] = column;
                dict_columns.Add(key_name, column);
            }

            return dict_columns;
        }
    }
}
