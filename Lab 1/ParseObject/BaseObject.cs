using lab1.MatrixOperations;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing.Imaging;

namespace Lab_1.ParseObject
{
    public abstract class BaseObject : IObject
    {
        public ObjectModel objectModel { get; }
        public float rotationX = 0;
        public float rotationY = 0;
        public float rotationZ = 0;
        public float scale = 1f;
        public float translationX = 0;
        public float translationY = 0;
        public float translationZ = 0;

        public float[,] rotateXMatrix, rotateYMatrix, rotateZMatrix, scaleMatrix, translationMatrix;
        public float[,] modelMatrix;

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

        public abstract float[,] GetModelMatrix();

        protected void SetInitialParams()
        {
            rotateXMatrix = Matricies.GetRotateXMatrix(rotationX);
            rotateYMatrix = Matricies.GetRotateYMatrix(rotationY);
            rotateZMatrix = Matricies.GetRotateZMatrix(rotationZ);
            scaleMatrix = Matricies.GetScaleMatrix(scale, scale, scale);
            translationMatrix = Matricies.GetTranslationMatrix(translationX, translationY, translationZ);

            modelMatrix = MultipleModelMatrix();
        }

        protected float[,] MultipleModelMatrix()
        {
            return MathsOperations.MultipleMatrix(
                        MathsOperations.MultipleMatrix(
                            MathsOperations.MultipleMatrix(
                                MathsOperations.MultipleMatrix(
                                    rotateZMatrix,
                                    rotateYMatrix),
                                rotateXMatrix),
                            scaleMatrix),
                        translationMatrix);
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