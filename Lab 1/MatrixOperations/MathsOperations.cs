using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lab1.MatrixOperations
{
    public class MathsOperations
    {
        public static float[,] MultipleMatrix(float[,] A, float[,] B)
        {
            int rowsA = A.GetLength(0);
            int colsA = A.GetLength(1);
            int rowsB = B.GetLength(0);
            int colsB = B.GetLength(1);

            if (colsA != rowsB) return null;

            float[,] C = new float[rowsA, colsB];
            for (int i = 0; i < rowsA; i++)
                for (int j = 0; j < colsB; j++)
                    for (int k = 0; k < rowsB; k++)
                        C[i,j] = C[i,j] + A[i,k] * B[k,j];
            return C;
        }

        public static Vector3 TransformVertex(Vector3 vertex, float[,] transformationMatrix)
        {
            float[] result = new float[4];

            result[0] = transformationMatrix[0, 0] * vertex.X + transformationMatrix[0, 1] * vertex.Y + transformationMatrix[0, 2] * vertex.Z + transformationMatrix[0, 3];
            result[1] = transformationMatrix[1, 0] * vertex.X + transformationMatrix[1, 1] * vertex.Y + transformationMatrix[1, 2] * vertex.Z + transformationMatrix[1, 3];
            result[2] = transformationMatrix[2, 0] * vertex.X + transformationMatrix[2, 1] * vertex.Y + transformationMatrix[2, 2] * vertex.Z + transformationMatrix[2, 3];
            result[3] = transformationMatrix[3, 0] * vertex.X + transformationMatrix[3, 1] * vertex.Y + transformationMatrix[3, 2] * vertex.Z + transformationMatrix[3, 3];
            return new Vector3(result[0], result[1], result[2]);
        }
    }

}