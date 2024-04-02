using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RevitExtensions
{

    public class CollectedElementsCollector
    {
        // Estructura de datos para representar el grafo de conexiones entre elementos
        private Dictionary<Element, List<Element>> adjacencyList = new Dictionary<Element, List<Element>>();

        public void AddConnection(Element element1, Element element2)
        {
            // Agregar conexiones entre elementos al grafo
            if (!adjacencyList.ContainsKey(element1))
            {
                adjacencyList[element1] = new List<Element>();
            }
            if (!adjacencyList.ContainsKey(element2))
            {
                adjacencyList[element2] = new List<Element>();
            }
            adjacencyList[element1].Add(element2);
            adjacencyList[element2].Add(element1);
        }

        public List<List<Element>> FindConnectedComponents()
        {
            List<List<Element>> connectedComponents = new List<List<Element>>();
            HashSet<Element> visited = new HashSet<Element>();

            foreach (Element element in adjacencyList.Keys)
            {
                if (!visited.Contains(element))
                {
                    // Iniciar una nueva búsqueda en profundidad o en anchura desde el elemento no visitado
                    List<Element> component = new List<Element>();
                    Explore(element, visited, component);
                    connectedComponents.Add(component);
                }
            }

            return connectedComponents;
        }

        private void Explore(Element currentElement, HashSet<Element> visited, List<Element> component)
        {
            // Agregar el elemento actual al componente conectado actual
            component.Add(currentElement);
            visited.Add(currentElement);

            // Explorar los elementos adyacentes al elemento actual
            foreach (Element neighbor in adjacencyList[currentElement])
            {
                if (!visited.Contains(neighbor))
                {
                    Explore(neighbor, visited, component);
                }
            }
        }

        public BoundingBoxXYZ CalculateBoundingBox(List<Element> elements)
        {
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double minZ = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;
            double maxZ = double.MinValue;

            foreach (Element element in elements)
            {
                BoundingBoxXYZ elementBoundingBox = GetElementBoundingBox(element);
                if (elementBoundingBox != null)
                {
                    XYZ min = elementBoundingBox.Min;
                    XYZ max = elementBoundingBox.Max;

                    minX = Math.Min(minX, min.X);
                    minY = Math.Min(minY, min.Y);
                    minZ = Math.Min(minZ, min.Z);

                    maxX = Math.Max(maxX, max.X);
                    maxY = Math.Max(maxY, max.Y);
                    maxZ = Math.Max(maxZ, max.Z);
                }
            }

            return new BoundingBoxXYZ
            {
                Min = new XYZ(minX, minY, minZ),
                Max = new XYZ(maxX, maxY, maxZ)
            };
        }


        private BoundingBoxXYZ GetElementBoundingBox(Element element)
        {
            // Implementar lógica para obtener el BoundingBox del elemento
            // Esto dependerá del tipo de elemento y cómo se almacena su ubicación y geometría en Revit
            // Por ejemplo, para una viga, podrías usar (element.Location as LocationCurve).Curve.ComputeBoundingBox()
            var boundingBox = element.get_BoundingBox(null);
            return boundingBox; // Debes implementar esta parte según tus necesidades específicas
        }
    }
}
