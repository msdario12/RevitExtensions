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

                var listOfCategories = new List<BuiltInCategory>
                {
                    BuiltInCategory.OST_StructuralFraming,
                    BuiltInCategory.OST_StructuralColumns,
                    BuiltInCategory.OST_StructuralFoundation
                };

                transaction.Start();
                //foreach (var category in listOfCategories)
                //{
                //    // Get filtered elements by category
                //    Dictionary<string, (BoundingBoxXYZ, XYZ)> dict_columns = GetElementsBasedInCategory(doc, category); // Create views
                //    createViewsBasedInElementCollection(doc, dict_columns);
                //}

                Dictionary<string, (BoundingBoxXYZ, XYZ)> beamGroups = FindConnectedBeams(doc);
                createViewsBasedInElementCollection(doc, beamGroups);

                transaction.Commit();
                return Result.Succeeded;

            }
            catch (Exception e)
            {
                message = e.Message;
                TaskDialog.Show("Error en Execute", $"{e.Message} \n {e.StackTrace}");
                transaction.RollBack();
                return Autodesk.Revit.UI.Result.Failed;
            }
        }

        public Dictionary<String, (BoundingBoxXYZ, XYZ)> FindConnectedBeams(Document doc)
        {
            Dictionary<String, (BoundingBoxXYZ, XYZ)> connectedBeamGroups = new Dictionary<String, (BoundingBoxXYZ, XYZ)>();

            // Recopilar todas las vigas del modelo
            FilteredElementCollector beamCollector = new FilteredElementCollector(doc);
            ICollection<Element> beams = beamCollector.OfCategory(BuiltInCategory.OST_StructuralFraming).WhereElementIsNotElementType().ToElements();

            int groupId = 1;

            var collectedElementoCollector = new CollectedElementsCollector();

            foreach (Element beam in beams)
            {
                bool grouped = false;

                // Obtener el BoundingBox de la viga actual
                BoundingBoxXYZ beamBoundingBox = beam.get_BoundingBox(null);

                foreach (Element insideBeam in beams)
                {
                    //if (beam.Id == insideBeam.Id)
                    //{
                    //    break;
                    //}

                    // Obtener el BoundingBox de la viga actual
                    BoundingBoxXYZ beamBoundingBoxInside = insideBeam.get_BoundingBox(null);

                    // Verificar si el BoundingBox de la viga actual se intersecta con el BoundingBox del grupo
                    Outline outline = new Outline(beamBoundingBoxInside.Min, beamBoundingBoxInside.Max);

                    BoundingBoxIntersectsFilter bbfilter = new BoundingBoxIntersectsFilter(outline);

                    // Use a view to construct the filter so we 
                    // get only visible elements. For example, 
                    // the analytical model will be found otherwise.

                    FilteredElementCollector collector = new FilteredElementCollector(doc, doc.ActiveView.Id);

                    // Lets also exclude the view itself (which 
                    // often will have an intersecting bounding box), 
                    // and also the element selected.

                    List<Element> filteredCollector = collector.WherePasses(bbfilter).OfCategory(BuiltInCategory.OST_StructuralFraming).ToList();
                    //bool isIn;

                    foreach (var item in filteredCollector)
                    {
                        XYZ beamOrientation = (beam as FamilyInstance).HandOrientation.Normalize();
                        XYZ insideBeamOrientation = (insideBeam as FamilyInstance).HandOrientation.Normalize();

                        bool isSameDirection = VectorComparer.AreVectorsInSameDirection(beamOrientation, insideBeamOrientation, 0.05);

                        if (item.Id == beam.Id && isSameDirection)
                        {
                            //connectedBeamGroups.Add(beam.Id.ToString(), (beamBoundingBox, (beam as FamilyInstance).HandOrientation));
                            collectedElementoCollector.AddConnection(beam, insideBeam);
                            groupId++;
                            break;
                        }
                    }

                }
            }

            List<List<Element>> connectedElements = collectedElementoCollector.FindConnectedComponents();
            foreach (var list in connectedElements)
            {
                string key = $"Portico_{groupId}";
                BoundingBoxXYZ groupBoundingBox;
                if (list.Count == 1)
                {
                    groupBoundingBox = list[0].get_BoundingBox(null);
                }
                else
                {
                    groupBoundingBox = collectedElementoCollector.CalculateBoundingBox(list);
                }

                XYZ facingVector = (list[0] as FamilyInstance).HandOrientation;

                connectedBeamGroups.Add(list[0].Id.ToString(), (groupBoundingBox, facingVector));
            }
            return connectedBeamGroups;
        }


        private static void createViewsBasedInElementCollection(Document doc, Dictionary<string, (BoundingBoxXYZ, XYZ)> boundingBoxDict)
        {
            // 💻 Create sections
            foreach (KeyValuePair<string, (BoundingBoxXYZ, XYZ)> keyValue in boundingBoxDict)
            {
                BoundingBoxXYZ boundingBox = keyValue.Value.Item1;

                BoundingBoxXYZ sectionBox = CreateSectionBoxBasedInBoundaryBox(boundingBox, keyValue.Value.Item2);

                //6 Create sections view.
                ElementId sectionTypeId = doc.GetDefaultElementTypeId(ElementTypeGroup.ViewTypeSection);
                ViewSection sectionView = ViewSection.CreateSection(doc, sectionTypeId, sectionBox);

                // Fine detail level
                sectionView.DetailLevel = ViewDetailLevel.Fine;
                sectionView.Scale = 50;

                //ElementId elementId = columnTuple.Value.GetTypeId();
                //ElementType type = doc.GetElement(elementId) as ElementType;

                //string viewName = $"API_{type.FamilyName}_{columnTuple.Key}";
                string viewName = keyValue.Key;
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
        public static XYZ GetPerpendicularVectorToLargestFace(BoundingBoxXYZ boundingBox)
        {
            // Calcular las dimensiones de cada cara del BoundingBox
            double lengthX = boundingBox.Max.X - boundingBox.Min.X;
            double lengthY = boundingBox.Max.Y - boundingBox.Min.Y;
            double lengthZ = boundingBox.Max.Z - boundingBox.Min.Z;

            // Determinar la mayor dimensión y la cara correspondiente
            if (lengthX >= lengthY && lengthX >= lengthZ)
            {
                // La cara de mayor dimensión es en el eje X
                return new XYZ(lengthX, 0, 0); // Vector paralelo al eje X
            }
            else if (lengthY >= lengthX && lengthY >= lengthZ)
            {
                // La cara de mayor dimensión es en el eje Y
                return new XYZ(0, lengthY, 0); // Vector paralelo al eje Y
            }
            else
            {
                // La cara de mayor dimensión es en el eje Z
                return new XYZ(0, 0, lengthZ); // Vector paralelo al eje Z
            }
        }

        private static BoundingBoxXYZ CreateSectionBoxBasedInBoundaryBox(BoundingBoxXYZ boundingBox, XYZ vector)
        {
            // Get origin of element
            XYZ column_origin = GetCenterBasedInBoundingBox(boundingBox);
            // Get vector
            //XYZ vector = GetPerpendicularVectorToLargestFace(boundingBox);
            //XYZ vector = new XYZ(boundingBox.Max.X, boundingBox.Min.Y, boundingBox.Max.Z) - new XYZ(boundingBox.Max.X,boundingBox.Min.Y,boundingBox.Min.Z) ;
            //XYZ vector = boundingBox.Min - new XYZ(boundingBox.Max.X, boundingBox.Min.Y, boundingBox.Min.Z);
            // Get element size

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
            return sectionBox;
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


        private static Dictionary<string, (BoundingBoxXYZ, XYZ)> GetElementsBasedInCategory(Document doc, BuiltInCategory category)
        {
            // 💻 Get all elements (columns) instances of each type.
            IList<Element> columns = new FilteredElementCollector(doc).
                OfCategory(category).
                WhereElementIsNotElementType().
                ToElements();

            Dictionary<string, (BoundingBoxXYZ, XYZ)> dict_columns = new Dictionary<string, (BoundingBoxXYZ, XYZ)>();

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

                XYZ vectorHost = (column as FamilyInstance).HandOrientation;
                dict_columns.Add(key_name, (column.get_BoundingBox(null), vectorHost));
            }

            return dict_columns;
        }
    }
}
