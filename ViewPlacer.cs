using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Linq;

namespace RevitExtensions
{
    internal class ViewPlacer
    {
        private Document _doc;
        private double _sheetWidth;
        private double _sheetHeight;
        private List<XYZ> _occupiedLocations;

        public ViewPlacer(Document doc, double sheetWidth, double sheetHeight)
        {
            _doc = doc;
            _sheetWidth = sheetWidth;
            _sheetHeight = sheetHeight;
            _occupiedLocations = new List<XYZ>();
        }

        public void PlaceViews(List<View> views, ViewSheet sheet, List<ViewSheet> createdSheets)
        {
            using (Transaction transaction = new Transaction(_doc, "Place Views"))
            {
                transaction.Start();

                foreach (var view in views)
                {
                    XYZ insertLocation = FindInsertLocation(view, sheet);
                    if (insertLocation != null)
                    {
                        PlaceView(view, sheet, insertLocation);
                        MarkOccupiedArea(view, insertLocation);
                    }
                    else
                    {
                        ViewSheet newSheet = CreateNewSheet();
                        if (newSheet != null)
                        {
                            createdSheets.Add(newSheet);
                            XYZ newInsertLocation = FindInsertLocation(view, newSheet);
                            if (newInsertLocation != null)
                            {
                                PlaceView(view, newSheet, newInsertLocation);
                                MarkOccupiedArea(view, newInsertLocation);
                            }
                            else
                            {
                                TaskDialog.Show("Error", "No se pudo encontrar espacio en la nueva hoja para colocar la vista.");
                            }
                        }
                        else
                        {
                            TaskDialog.Show("Error", "No se pudo crear una nueva hoja para colocar la vista.");
                        }
                    }
                }

                transaction.Commit();
            }
        }

        private XYZ FindInsertLocation(View view, ViewSheet sheet)
        {
            // Ordena las ubicaciones ocupadas para buscar el siguiente punto de inserción
            var sortedLocations = _occupiedLocations.OrderBy(loc => loc.Y).ThenBy(loc => loc.X);

            foreach (var location in sortedLocations)
            {
                double rightX = location.X + view.Outline.Max.U;
                double bottomY = location.Y - view.Outline.Max.V;

                if (rightX <= _sheetWidth && bottomY >= 0)
                {
                    // Se ha encontrado un espacio disponible
                    return new XYZ(location.X, location.Y, 0);
                }
            }

            return null; // No se encontró un espacio disponible
        }

        private void PlaceView(View view, ViewSheet sheet, XYZ insertLocation)
        {
            // Crea un elemento Viewport para la vista y lo coloca en la hoja en la ubicación especificada
            Viewport viewport = Viewport.Create(_doc, sheet.Id, view.Id, insertLocation);
        }

        private void MarkOccupiedArea(View view, XYZ insertLocation)
        {
            double rightX = insertLocation.X + view.Outline.Max.U;
            double bottomY = insertLocation.Y - view.Outline.Max.V;

            // Marca el área ocupada por la vista
            _occupiedLocations.Add(new XYZ(insertLocation.X, insertLocation.Y, 0)); // Esquina superior izquierda
            _occupiedLocations.Add(new XYZ(rightX, insertLocation.Y, 0)); // Esquina superior derecha
            _occupiedLocations.Add(new XYZ(insertLocation.X, bottomY, 0)); // Esquina inferior izquierda
            _occupiedLocations.Add(new XYZ(rightX, bottomY, 0)); // Esquina inferior derecha
        }

        private ViewSheet CreateNewSheet()
        {
            // Crea una nueva hoja y la devuelve
            // Esto dependerá de cómo se crean las hojas en tu aplicación
            // Retorna la nueva hoja creada, o null si no se pudo crear
            return null;
        }
    }
}
