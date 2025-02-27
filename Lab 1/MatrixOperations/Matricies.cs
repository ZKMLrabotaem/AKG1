

namespace lab1.MatrixOperations
{
    public class Matricies
    {
        // [𝑉𝑖𝑒𝑤𝑝𝑜𝑟𝑡] × [𝑃𝑟𝑜𝑗𝑒𝑐𝑡𝑖𝑜𝑛] × [𝑉𝑖𝑒𝑤] × [𝑀𝑜𝑑𝑒𝑙]

        // [𝑀𝑜𝑑𝑒𝑙]
        public static float[,] GetTranslationMatrix(float tx, float ty, float tz)
        {
            float[,] translationMatrix =
            {
                { 1, 0, 0, tx },
                { 0, 1, 0, ty },
                { 0, 0, 1, tz },
                { 0, 0, 0, 1 }
            };
            return translationMatrix;
        }

        public static float[,] GetScaleMatrix(float sx, float sy, float sz)
        {
            float[,] scaleMatrix =
            {
                { sx, 0, 0, 0 },
                { 0, sy, 0, 0 },
                { 0, 0, sz, 0 },
                { 0, 0, 0, 1 }
            };
            return scaleMatrix;
        }

        public static float[,] GetRotateXMatrix(float alpha)
        {
            float[,] rotateXMatrix =
            {
                { 1, 0, 0, 0 },
                { 0, (float)Math.Cos(alpha), -(float)Math.Sin(alpha), 0 },
                { 0, (float)Math.Sin(alpha), (float)Math.Cos(alpha), 0 },
                { 0, 0, 0, 1 }
            };
            return rotateXMatrix;
        }

        public static float[,] GetRotateYMatrix(float alpha)
        {
            float[,] rotateYMatrix =
            {
                { (float) Math.Cos(alpha), 0, (float) Math.Sin(alpha), 0 },
                { 0, 1, 0, 0 },
                { -(float) Math.Sin(alpha), 0, (float) Math.Cos(alpha), 0 },
                { 0, 0, 0, 1 }
            };
            return rotateYMatrix;
        }

        public static float[,] GetRotateZMatrix(float alpha)
        {
            float[,] rotateZMatrix =
            {
                { (float)Math.Cos(alpha), -(float)Math.Sin(alpha), 0, 0 },
                { (float)Math.Sin(alpha), (float)Math.Cos(alpha), 0, 0 },
                { 0, 0, 1, 0 },
                { 0, 0, 0, 1 }
            };
            return rotateZMatrix;
        }

        // [𝑉𝑖𝑒𝑤]
        public static float[,] GetObserverMatrix(Vector3 eye, Vector3 target, Vector3 up)
        {
            Vector3 ZAxis = (eye - target).Normalize();
            Vector3 XAxis = Vector3.VectorMultiplication(up, ZAxis).Normalize();
            Vector3 YAxis = up;

            float[,] watcherMatrix = {
                { XAxis.X, XAxis.Y, XAxis.Z, -Vector3.ScalarMultiplication(XAxis, eye) },
                { YAxis.X, YAxis.Y, YAxis.Z, -Vector3.ScalarMultiplication(YAxis, eye) },
                { ZAxis.X, ZAxis.Y, ZAxis.Z, -Vector3.ScalarMultiplication(ZAxis, eye) },
                { 0, 0, 0, 1 }
            };
            return watcherMatrix;
        }

        // [𝑃𝑟𝑜𝑗𝑒𝑐𝑡𝑖𝑜𝑛]
        public static float[,] GetProjectionSpaceMatrix(float width, float heigth, float Znear, float Zfar)
        {
            float[,] projectionSpaceMatrix = {
                { 2/width, 0, 0, 0 },
                { 0, 2/heigth, 0, 0 },
                { 0, 0, 1/(Znear - Zfar), Znear/(Znear - Zfar) },
                { 0, 0, 0, 1 }
            };
            return projectionSpaceMatrix;
        }

        public static float[,] GetPerspectiveProjectionMatrix(float fov, float aspect, float Znear, float Zfar)
        {
            float tanHalfFOV = (float)Math.Tan(fov / 2);

            float[,] perspectiveProjectionMatrix = {
                { 1 / (tanHalfFOV * aspect), 0, 0, 0 },
                { 0, 1 / tanHalfFOV, 0, 0 },
                { 0, 0, Zfar / (Znear - Zfar), (Znear * Zfar) / (Znear - Zfar) },
                { 0, 0, -1, 0 }
            };

            return perspectiveProjectionMatrix;
        }


        // [𝑉𝑖𝑒𝑤𝑝𝑜𝑟𝑡]
        public static float[,] GetViewingWindowMatrix(float width, float height, float Xmin, float Ymin)
        {
            float[,] viewingWindowMatrix = {
                { width/2, 0, 0, Xmin + width/2 },
                { 0, -height/2, 0, Ymin + height/2 },
                { 0, 0, 1, 0 },
                { 0, 0, 0, 1 }
            };
            return viewingWindowMatrix;
        }
    }
}
