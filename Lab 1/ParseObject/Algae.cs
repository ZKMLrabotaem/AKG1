using System;
using System.Collections.Generic;
using System.Drawing.Imaging;

namespace Lab_1.ParseObject
{
    public class Algae : BaseObject
    {
        private const string AlgaeColorPath = "Objects\\algae_color.png";

        public Algae(string filePath) : base(filePath)
        {
            LoadTextures();
        }

        protected override void LoadTextures()
        {
            diffuseMap = LoadTexture(AlgaeColorPath);

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
    }
}