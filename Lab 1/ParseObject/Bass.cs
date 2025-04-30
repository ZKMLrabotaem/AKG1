/*using Aspose.ThreeD.Shading;
using Lab_1.ParseObject;
using lab1.Structures;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lab1.ParseObject
{
    public class Bass : IObject
    {
        public ObjectModel objectModel { get; }

        private const string BassColorPath = "Objects\\bass_color.png";
        private const string BassNormalsPath = "Objects\\bass_normal.png";
        private const string BassSpecularPath = "Objects\\bass_specular.png";

        private float minZ = float.MaxValue;
        private float maxTailZ = float.MinValue;

        private List<Vector3> currentVertices;
        private List<int> tailVerticesIndices = new List<int>();

        private float amplitude = 0.2f;
        private float frequency = 3f;
        private float time = 0.0f;

        private List<Vector3> previousOffsets;
        private float damping = 0.98f;
        private float waveSpeed = 4.0f;

        private System.Windows.Forms.Timer animationTimer;
        private DateTime lastFrameTime = DateTime.Now;
        private DateTime currentFrameTime = DateTime.Now;
        private float deltaTime;

        public Bitmap diffuseMap, normalMap, specularMap;
        public BitmapData bmpDataDiffuse, bmpDataNormal, bmpDataSpecular;

        public Bass(string filePath)
        {
            objectModel = Parsing.ParseObject(filePath, out float minZ);
            this.minZ = minZ;

            SetCurrentVertices();
            CreateAnimationTimer();
            LoadTextures(); 
        }

        private void LoadTextures()
        {
            diffuseMap = LoadTexture(BassColorPath);
            //normalMap = LoadTexture(BassNormalsPath);
            //specularMap = LoadTexture(BassSpecularPath);

            if (diffuseMap != null)
            {
                bmpDataDiffuse = diffuseMap.LockBits(
                    new Rectangle(0, 0, diffuseMap.Width, diffuseMap.Height),
                    ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            }
            if (normalMap != null)
            {
                bmpDataNormal = normalMap.LockBits(
                   new Rectangle(0, 0, normalMap.Width, normalMap.Height),
                   ImageLockMode.ReadOnly,
                   System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            }
            if (specularMap != null)
            {
                bmpDataSpecular = specularMap.LockBits(
                    new Rectangle(0, 0, specularMap.Width, specularMap.Height),
                    ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            }
        }

        private Bitmap? LoadTexture(string filePath)
        {
            try { return new Bitmap(filePath); }
            catch { return null; }
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

        public List<Vector3> GetCurrentVertices()
        {
            return currentVertices;
        }
    }
}*/








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