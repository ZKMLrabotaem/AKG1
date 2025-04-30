using System;
using System.Collections.Generic;
using System.Drawing.Imaging;

namespace Lab_1.ParseObject
{
    public abstract class BaseObject : IObject
    {
        public ObjectModel objectModel { get; }
        protected List<Vector3> currentVertices { get; set; }
        protected float minZ;

        public Bitmap diffuseMap, normalMap, specularMap;
        public BitmapData bmpDataDiffuse, bmpDataNormal, bmpDataSpecular;

        protected BaseObject(string filePath)
        {
            objectModel = Parsing.ParseObject(filePath, out float minZ);
            this.minZ = minZ;
        }

        public virtual List<Vector3> GetCurrentVertices()
        {
            return currentVertices;
        }

        protected abstract void LoadTextures();

        protected Bitmap? LoadTexture(string filePath)
        {
            try { return new Bitmap(filePath); }
            catch { return null; }
        }

        protected void UnlockTextures()
        {
            if (diffuseMap != null) diffuseMap.UnlockBits(bmpDataDiffuse);
            if (normalMap != null) normalMap.UnlockBits(bmpDataNormal);
            if (specularMap != null) specularMap.UnlockBits(bmpDataSpecular);
        }
    }
}