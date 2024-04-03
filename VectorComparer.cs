using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitExtensions
{
    public class VectorComparer
    {
        public static bool AreVectorsInSameDirection(XYZ vector1, XYZ vector2, double toleranceDegrees)
        {
            // Normalizar los vectores para asegurarse de que tengan la misma dirección
            vector1 = vector1.Normalize();
            vector2 = vector2.Normalize();

            // Calcular el producto punto entre los vectores
            double dotProduct = vector1.DotProduct(vector2);

            // Calcular el ángulo entre los vectores en radianes
            double angle = Math.Acos(dotProduct);

            // Convertir el ángulo a grados
            double angleDegrees = angle * (180 / Math.PI);

            // Verificar si el ángulo está dentro de la tolerancia especificada
            return angleDegrees <= toleranceDegrees;
        }
    }
}
