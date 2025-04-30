using System;
using System.Collections.Generic;
using System.Drawing.Imaging;

namespace Lab_1.ParseObject
{
    public class Sand : BaseObject
    {
        private const string SandColorPath = "Objects\\sand_color.jpg";

        public Sand(string filePath) : base(filePath)
        {
            LoadTextures();
            scale = 2f;
            translationY = -5f;
            SetInitialParams();
        }

        protected override void LoadTextures()
        {
            diffuseMap = LoadTexture(SandColorPath);

            if (diffuseMap != null)
            {
                bmpDataDiffuse = diffuseMap.LockBits(
                    new Rectangle(0, 0, diffuseMap.Width, diffuseMap.Height),
                    ImageLockMode.ReadOnly,
                    PixelFormat.Format24bppRgb);
            }
        }

        public override List<Vector3> GetCurrentVertices()
        {
            return new List<Vector3>(objectModel.Vertices); 
        }

        public override float[,] GetModelMatrix()
        {
            return modelMatrix;//MultipleModelMatrix();
        }
    }
}