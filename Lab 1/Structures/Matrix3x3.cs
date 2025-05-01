using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab_1.Structures
{
    public class Matrix3x3
    {
        private float[,] m = new float[3, 3];

        public Matrix3x3(
            float m00, float m01, float m02,
            float m10, float m11, float m12,
            float m20, float m21, float m22)
        {
            m[0, 0] = m00; m[0, 1] = m01; m[0, 2] = m02;
            m[1, 0] = m10; m[1, 1] = m11; m[1, 2] = m12;
            m[2, 0] = m20; m[2, 1] = m21; m[2, 2] = m22;
        }

        public Vector3 Transform(Vector3 v)
        {
            return new Vector3(
                m[0, 0] * v.X + m[0, 1] * v.Y + m[0, 2] * v.Z,
                m[1, 0] * v.X + m[1, 1] * v.Y + m[1, 2] * v.Z,
                m[2, 0] * v.X + m[2, 1] * v.Y + m[2, 2] * v.Z
            );
        }
    }
}
