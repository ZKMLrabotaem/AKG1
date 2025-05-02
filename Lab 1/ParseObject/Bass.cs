using lab1.MatrixOperations;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace Lab_1.ParseObject
{
    public class Bass : BaseObject
    {
        private const string BassColorPath = "Objects\\bass_color.png";
        private float maxTailZ = float.MinValue;
        private List<int> tailVerticesIndices = new List<int>();
        private List<Vector3> previousOffsets;
        private System.Windows.Forms.Timer animationTimer;
        private DateTime lastFrameTime = DateTime.Now;
        private DateTime currentFrameTime = DateTime.Now;
        private float deltaTime;

        private float amplitude = 0.2f;
        private float frequency = 3f;
        private float time = 0.0f;
        private float damping = 0.98f;
        private float waveSpeed = 4.0f;

        public Bass(string filePath) : base(filePath)
        {
            SetCurrentVertices();
            CreateAnimationTimer();
            LoadTextures();
            scale = 8f;
            SetInitialParams();
        }

        public override float[,] GetModelMatrix()
        {
            return MultipleModelMatrix();
        }

        protected override void LoadTextures()
        {
            diffuseMap = LoadTexture(BassColorPath);

            if (diffuseMap != null)
            {
                bmpDataDiffuse = diffuseMap.LockBits(
                    new Rectangle(0, 0, diffuseMap.Width, diffuseMap.Height),
                    ImageLockMode.ReadOnly,
                    PixelFormat.Format24bppRgb);
            }
        }

        public void ChangeModelMatrix(float translationX, float transationY, float transationZ)
        {
            this.translationX = translationX;
            this.translationY = transationY;
            this.translationZ = transationZ;
            SetInitialParams();
        }

        private void SetCurrentVertices()
        {
            float tailThreshold = minZ + (objectModel.Vertices.Max(v => v.Z) - minZ) * 1f;

            for (int j = 0; j < objectModel.Vertices.Count; j++)
            {
                if (objectModel.Vertices[j].Z <= tailThreshold)
                {
                    tailVerticesIndices.Add(j);
                    if (maxTailZ < objectModel.Vertices[j].Z) maxTailZ = objectModel.Vertices[j].Z;
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

            for (int i = 0; i < tailVerticesIndices.Count; i++)
            {
                int index = tailVerticesIndices[i];

                float distanceFromBase = objectModel.Vertices[index].Z - maxTailZ;

                float phase = frequency * time - distanceFromBase * waveSpeed;

                float offsetX = amplitude * distanceFromBase *
                                (float)Math.Sin(phase);

                Vector3 previousOffset = previousOffsets[index];
                Vector3 targetOffset = new Vector3(offsetX, 0, 0);
                Vector3 smoothedOffset = Vector3.Lerp(previousOffset, targetOffset, 0.2f);
                smoothedOffset *= damping;

                previousOffsets[index] = smoothedOffset;

                Vector3 vertex = currentVertices[index];
                vertex += smoothedOffset;
                currentVertices[index] = vertex;
            }
        }

        public override List<Vector3> GetCurrentVertices()
        {
            return new List<Vector3>(currentVertices);
        }

        ~Bass()
        {
            UnlockTextures();
        }
    }
}