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

                var listOfCategories = new List<BuiltInCategory>();
                listOfCategories.Add(BuiltInCategory.OST_StructuralFraming);
                listOfCategories.Add(BuiltInCategory.OST_StructuralColumns);
                listOfCategories.Add(BuiltInCategory.OST_StructuralFoundation);

                transaction.Start();
                foreach (var category in listOfCategories)
                {
                    // Get filtered elements by category
                    Dictionary<string, Element> dict_columns = GetElementsBasedInCategory(doc, category); // Create views
                    createViewsBasedInElementCollection(doc, dict_columns);
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
        private static void createViewsBasedInElementCollection(Document doc, Dictionary<string, Element> dict_columns)
        {
            // 💻 Create sections
            foreach (KeyValuePair<string, Element> columnTuple in dict_columns)
            {
                BoundingBoxXYZ boundingBox = columnTuple.Value.get_BoundingBox(null);

                // Get origin of element
                XYZ column_origin = GetCenterBasedInBoundingBox(boundingBox);

                XYZ faceOrientation = (columnTuple.Value as FamilyInstance).HandOrientation;
                XYZ vector = faceOrientation;
                // Get element size
                ElementId elementId = columnTuple.Value.GetTypeId();
                ElementType type = doc.GetElement(elementId) as ElementType;

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

                sectionBox.Min = new XYZ(-half - offset, 0 - elementHeight / 2 - offset, -elementDepth);
                sectionBox.Max = new XYZ(+half + offset, elementHeight / 2 + offset, elementDepth);
                // Apply transforms to section box
                sectionBox.Transform = transformIdentity;

                //6 Create sections view.
                ElementId sectionTypeId = doc.GetDefaultElementTypeId(ElementTypeGroup.ViewTypeSection);
                ViewSection sectionView = ViewSection.CreateSection(doc, sectionTypeId, sectionBox);
                // Fine detail level
                sectionView.DetailLevel = ViewDetailLevel.Fine;
                sectionView.Scale = 50;

                string viewName = $"API_{type.FamilyName}_{columnTuple.Key}";
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
        }
        public static XYZ GetCenterBasedInBoundingBox(BoundingBoxXYZ boundingBox)
        {
            // Obtener los puntos mínimo y máximo del BoundingBox
            XYZ minPoint = boundingBox.Min;
            XYZ maxPoint = boundingBox.Max;

            // Calcular el centro como el promedio de los puntos mínimo y máximo
            double centerX = (minPoint.X + maxPoint.X) / 2;
            double centerY = (minPoint.Y + maxPoint.Y) / 2;
            double centerZ = (minPoint.Z + maxPoint.Z) / 2;

            // Crear y devolver el centro como un objeto XYZ
            XYZ center = new XYZ(centerX, centerY, centerZ);
            return center;
        }
        public static XYZ GetCenterOfBeam(Curve curve)
        {
            // Obtener los puntos finales de la curva
            XYZ startPoint = curve.GetEndPoint(0);
            XYZ endPoint = curve.GetEndPoint(1);

            // Calcular el punto medio entre los puntos finales
            double centerX = (startPoint.X + endPoint.X) / 2;
            double centerY = startPoint.Y;
            double centerZ = (startPoint.Z + endPoint.Z) / 2;

            // Crear y devolver el punto medio como un objeto XYZ
            XYZ center = new XYZ(centerX, centerY, centerZ);
            return center;
        }


        private static Dictionary<string, Element> GetElementsBasedInCategory(Document doc, BuiltInCategory category)
        {
            // 💻 Get all elements (columns) instances of each type.
            IList<Element> columns = new FilteredElementCollector(doc).
                OfCategory(category).
                WhereElementIsNotElementType().
                ToElements();

            Dictionary<string, Element> dict_columns = new Dictionary<string, Element>();

            int counter = 0;
            foreach (var column in columns)
            {
                ElementId elementId = column.GetTypeId();
                ElementType type = doc.GetElement(elementId) as ElementType;
                counter++;
                //String numeration = column.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).AsString();
                string numeration = counter.ToString();
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
