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
            return new Vector3(result[0], result[1], result[2], result[3]);
        }

        public static float[,] InverseTransposeMatrix(float[,] matrix)
        {
            if (matrix.GetLength(0) != 4 || matrix.GetLength(1) != 4)
                throw new ArgumentException("Matrix must be 4x4");

            float[,] inverse = InverseMatrix(matrix);

            float[,] inverseTranspose = new float[4, 4];
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    inverseTranspose[i, j] = inverse[j, i];
                }
            }

            return inverseTranspose;
        }

        private static float[,] InverseMatrix(float[,] matrix)
        {
            float tx = matrix[0, 3];
            float ty = matrix[1, 3];
            float tz = matrix[2, 3];

            float[,] upper3x3 = new float[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    upper3x3[i, j] = matrix[i, j];
                }
            }

            float[,] upper3x3Inv = Inverse3x3Matrix(upper3x3);

            float[,] inverse = new float[4, 4];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    inverse[i, j] = upper3x3Inv[i, j];
                }
            }

            inverse[0, 3] = -(upper3x3Inv[0, 0] * tx + upper3x3Inv[0, 1] * ty + upper3x3Inv[0, 2] * tz);
            inverse[1, 3] = -(upper3x3Inv[1, 0] * tx + upper3x3Inv[1, 1] * ty + upper3x3Inv[1, 2] * tz);
            inverse[2, 3] = -(upper3x3Inv[2, 0] * tx + upper3x3Inv[2, 1] * ty + upper3x3Inv[2, 2] * tz);

            inverse[3, 0] = 0;
            inverse[3, 1] = 0;
            inverse[3, 2] = 0;
            inverse[3, 3] = 1;

            return inverse;
        }

        private static float[,] Inverse3x3Matrix(float[,] matrix)
        {
            float det = matrix[0, 0] * (matrix[1, 1] * matrix[2, 2] - matrix[1, 2] * matrix[2, 1]) -
                        matrix[0, 1] * (matrix[1, 0] * matrix[2, 2] - matrix[1, 2] * matrix[2, 0]) +
                        matrix[0, 2] * (matrix[1, 0] * matrix[2, 1] - matrix[1, 1] * matrix[2, 0]);

            if (Math.Abs(det) < float.Epsilon)
                throw new InvalidOperationException("Matrix is not invertible");

            float invDet = 1.0f / det;

            float[,] inverse = new float[3, 3];
            inverse[0, 0] = (matrix[1, 1] * matrix[2, 2] - matrix[1, 2] * matrix[2, 1]) * invDet;
            inverse[0, 1] = (matrix[0, 2] * matrix[2, 1] - matrix[0, 1] * matrix[2, 2]) * invDet;
            inverse[0, 2] = (matrix[0, 1] * matrix[1, 2] - matrix[0, 2] * matrix[1, 1]) * invDet;

            inverse[1, 0] = (matrix[1, 2] * matrix[2, 0] - matrix[1, 0] * matrix[2, 2]) * invDet;
            inverse[1, 1] = (matrix[0, 0] * matrix[2, 2] - matrix[0, 2] * matrix[2, 0]) * invDet;
            inverse[1, 2] = (matrix[0, 2] * matrix[1, 0] - matrix[0, 0] * matrix[1, 2]) * invDet;

            inverse[2, 0] = (matrix[1, 0] * matrix[2, 1] - matrix[1, 1] * matrix[2, 0]) * invDet;
            inverse[2, 1] = (matrix[0, 1] * matrix[2, 0] - matrix[0, 0] * matrix[2, 1]) * invDet;
            inverse[2, 2] = (matrix[0, 0] * matrix[1, 1] - matrix[0, 1] * matrix[1, 0]) * invDet;

            return inverse;
        }
    }

}