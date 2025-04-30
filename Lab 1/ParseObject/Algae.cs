using lab1.MatrixOperations;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace Lab_1.ParseObject
{
    public class Algae : BaseObject
    {
        private const string AlgaeColorPath = "Objects\\algae_color.jpg";

        private List<int> topVerticesIndices = new List<int>();
        private List<Vector3> previousOffsets;
        private System.Windows.Forms.Timer animationTimer;
        private DateTime lastFrameTime = DateTime.Now;
        private DateTime currentFrameTime = DateTime.Now;
        private float deltaTime;

        private float amplitudeY = 0.1f; 
        private float amplitudeX = 0.1f; 
        private float frequency = 2f;   
        private float time = 0.0f;
        private float damping = 0.9f;   

        public Algae(string filePath) : base(filePath)
        {
            LoadTextures();
            scale = 1f;
            SetTopVertices();
            CreateAnimationTimer();
        }

        public void ChangeModelMatrix(
            float translationX, float translationY, float translationZ)
        {
            this.translationX = translationX;
            this.translationY = translationY;
            this.translationZ = translationZ;
            SetInitialParams();
        }

        protected override void LoadTextures()
        {
            diffuseMap = LoadTexture(AlgaeColorPath);

            if (diffuseMap != null)
            {
                bmpDataDiffuse = diffuseMap.LockBits(
                    new System.Drawing.Rectangle(0, 0, diffuseMap.Width, diffuseMap.Height),
                    ImageLockMode.ReadOnly,
                    PixelFormat.Format24bppRgb);
            }
        }

        private void SetTopVertices()
        {
            float maxY = float.MinValue;
            foreach (var v in objectModel.Vertices)
            {
                if (v.Y > maxY) maxY = v.Y;
            }

            float threshold = maxY - (maxY - objectModel.Vertices.Min(v => v.Y)) * 0.8f;

            for (int i = 0; i < objectModel.Vertices.Count; i++)
            {
                if (objectModel.Vertices[i].Y >= threshold)
                {
                    topVerticesIndices.Add(i);
                }
            }

            currentVertices = new List<Vector3>(objectModel.Vertices);
            previousOffsets = new List<Vector3>(new Vector3[objectModel.Vertices.Count]);
        }

        private void CreateAnimationTimer()
        {
            animationTimer = new System.Windows.Forms.Timer
            {
                Interval = 50
            };
            animationTimer.Tick += AnimationTimer_Tick;
            animationTimer.Start();
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            currentFrameTime = DateTime.Now;
            deltaTime = (float)(currentFrameTime - lastFrameTime).TotalSeconds;
            lastFrameTime = currentFrameTime;
            Update(deltaTime);
        }

        private void Update(float deltaTime)
        {
            time += deltaTime;
            currentVertices = new List<Vector3>(objectModel.Vertices);

            foreach (int index in topVerticesIndices)
            {
                var vertex = objectModel.Vertices[index];
                float offsetY = amplitudeY * (float)Math.Sin(frequency * time + vertex.X * 5.0f);

                float offsetX = amplitudeX * (float)Math.Cos(frequency * time + vertex.Z * 5.0f);

                Vector3 targetOffset = new Vector3(offsetX, offsetY, 0);

                Vector3 previousOffset = previousOffsets[index];
                Vector3 smoothedOffset = Vector3.Lerp(previousOffset, targetOffset, 0.2f);
                smoothedOffset *= damping;

                previousOffsets[index] = smoothedOffset;

                currentVertices[index] = vertex + smoothedOffset;
            }
        }

        public override List<Vector3> GetCurrentVertices()
        {
            return new List<Vector3>(currentVertices);
        }

        public override float[,] GetModelMatrix()
        {
            return MultipleModelMatrix();
        }

        ~Algae()
        {
            UnlockTextures();
        }
    }
}