using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using lab1.MatrixOperations;
using lab1.ParseObject;
using lab1.Structures;

namespace lab1.Forms
{
    public partial class Scene : Form
    {
        private ObjModel obj;
        private float rotationX = 0;
        private float rotationY = 0;
        private float rotationZ = 0;
        private float scale = 8f;
        private float translationX = 0;
        private float translationY = 0;
        private float translationZ = 0;

        private float[,] rotateXMatrix, rotateYMatrix, rotateZMatrix, scaleMatrix, translationMatrix;
        private float[,] modelMatrix, observerMatrix, projectionMatrix, viewportMatrix, resultMatrix;

        private Vector3 eye = new Vector3(0, 0, 5);
        private Vector3 target = new Vector3(0, 0, 0);
        private Vector3 up = new Vector3(0, 1, 0);
        private Vector3 lightDirection = new Vector3(-1, -1, -1).Normalize();

        private Bitmap diffuseMap;
        private Bitmap normalMap;
        private Bitmap specularMap;

        private BitmapData bmpDataDiffuse, bmpDataNormal, bmpDataSpecular;

        private const float RotationSpeed = 2f;
        private const float TranslationSpeed = 0.3f;
        private const float MouseWheelSpeed = 0.0005f;
        private System.Windows.Forms.Timer movementTimer;
        private System.Windows.Forms.Timer animationTimer;

        private const string BassObjPath = "Objects\\bass_object.obj";
        private const string BassColorPath = "Objects\\bass_color.png";
        private const string BassNormalsPath = "Objects\\bass_normal.png";

        private HashSet<Keys> pressedKeys = new HashSet<Keys>();

        private float[,] zBuffer;
        private Bitmap bitmap;

        private readonly Vector3[] verticesInViewport = new Vector3[3];
        private readonly Vector3[] verticesInWorld = new Vector3[3];
        private readonly Vector3[] vertexNormals = new Vector3[3];
        private float[] invW = new float[3];
        private  Vector2 uv0 = default, uv1 = default, uv2 = default;

        private int objectMode = 1;

        private DateTime lastFrameTime = DateTime.Now;
        private DateTime lastAnimationUpdateTime = DateTime.Now;
        private float deltaTime = 0;

        public enum LightingMode
        {
            Lambert,
            Phong,
            Texture
        }

        public LightingMode CurrentLightingMode { get; set; } = LightingMode.Texture;

        public Scene()
        {
            InitializeComponent();
            obj = new ObjModel(BassObjPath);
            this.KeyDown += Scene_KeyDown;
            this.KeyUp += Scene_KeyUp;
            movementTimer = new System.Windows.Forms.Timer
            {
                Interval = 8
            };
            movementTimer.Tick += MovementTimer_Tick;
            movementTimer.Start();

            animationTimer = new System.Windows.Forms.Timer
            {
                Interval = 50
            };
            animationTimer.Tick += AnimationTimer_Tick;
            animationTimer.Start();

            rotateXMatrix = Matricies.GetRotateXMatrix(rotationX);
            rotateYMatrix = Matricies.GetRotateYMatrix(rotationY);
            rotateZMatrix = Matricies.GetRotateZMatrix(rotationZ);
            scaleMatrix = Matricies.GetScaleMatrix(scale, scale, scale);
            translationMatrix = Matricies.GetTranslationMatrix(translationX, translationY, translationZ);
            CreateBitmap();
            lightDirection = lightDirection * -1;

            diffuseMap = LoadTexture(BassColorPath);
            //normalMap = LoadTexture(BassNormalsPath);

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

            UpdateScene();
        }

        private void Scene_KeyDown(object sender, KeyEventArgs e)
        {
            pressedKeys.Add(e.KeyCode);

            if (!movementTimer.Enabled)
            {
                movementTimer.Start();
            }
            if (e.KeyCode == Keys.L)
            {
                CurrentLightingMode = (LightingMode)(((int)CurrentLightingMode + 1) % Enum.GetValues(typeof(LightingMode)).Length);
                UpdateScene();
            }
        }

        private void Scene_KeyUp(object sender, KeyEventArgs e)
        {
            pressedKeys.Remove(e.KeyCode);

            /*if (pressedKeys.Count == 0)
            {
                movementTimer.Stop();
            }*/
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            var scrollAmount = e.Delta * MouseWheelSpeed;
            if (ModifierKeys.HasFlag(Keys.Alt))
            {
                eye.X += scrollAmount / 3f;
                target.X += scrollAmount / 3f;
            }
            else if (ModifierKeys.HasFlag(Keys.Control))
            {
                eye.Y += scrollAmount / 3f;
                target.Y = Math.Max(0.1f, eye.Y);
            }
            else
            {
                eye.Z -= scrollAmount * 20f;
                target.Z -= scrollAmount * 20f;
            }
            //UpdateScene();
        }
        private void MovementTimer_Tick(object sender, EventArgs e)
        {
            DateTime currentFrameTime = DateTime.Now;
            deltaTime = (float)(currentFrameTime - lastFrameTime).TotalSeconds;
            lastFrameTime = currentFrameTime;

            bool isCtrlPressed = (Control.ModifierKeys & Keys.Control) == Keys.Control;
            foreach (var key in pressedKeys.ToList())
            {
                HandleKeyPress(key, isCtrlPressed, deltaTime);
            }
            UpdateScene();
        }
        private void Scene_Resize(object sender, EventArgs e)
        {
            CreateBitmap();
        }

        private void CreateBitmap()
        {
            bitmap?.Dispose();
            bitmap = new Bitmap(pictureBox1.Width, pictureBox1.Height, PixelFormat.Format24bppRgb);
            zBuffer = new float[pictureBox1.Width, pictureBox1.Height];
        }
        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            DateTime currentFrameTime = DateTime.Now;
            deltaTime = (float)(currentFrameTime - lastAnimationUpdateTime).TotalSeconds;
            lastAnimationUpdateTime = currentFrameTime;
            obj.Update(deltaTime);
            //UpdateScene();
        }


        protected void UpdateScene()
        {
 
            ClearBitmap(bitmap);
            InitializeZBuffer();

            modelMatrix = MathsOperations.MultipleMatrix(
                MathsOperations.MultipleMatrix(
                    MathsOperations.MultipleMatrix(
                        MathsOperations.MultipleMatrix(
                            rotateZMatrix,
                            rotateYMatrix),
                        rotateXMatrix),
                    scaleMatrix),
                translationMatrix);

            observerMatrix = Matricies.GetObserverMatrix(eye, target, up);
            projectionMatrix = Matricies.GetPerspectiveProjectionMatrix((float)Math.PI / 2f, (float)bitmap.Width / bitmap.Height, 1f, 1000f);
            viewportMatrix = Matricies.GetViewingWindowMatrix(bitmap.Width, bitmap.Height, 0, 0);

            resultMatrix = MathsOperations.MultipleMatrix(
                MathsOperations.MultipleMatrix(
                    MathsOperations.MultipleMatrix(
                        viewportMatrix,
                        projectionMatrix),
                    observerMatrix),
                modelMatrix);

            int faceCount = obj.faces.Length / 3;
          
            var currentVerticies = obj.GetAnimatedVertices();

            for (int i = 0; i < faceCount; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    int vIndex = obj.faces[i * 3 + j] - 1;
                    int nIndex = obj.normals[i * 3 + j] - 1;

                    Vector3 vScreen = MathsOperations.TransformVertex(/*obj.Vertices[vIndex]*/currentVerticies[vIndex], resultMatrix);
                    if (vScreen.W == 0) vScreen.W = 1e-6f; 

                    verticesInViewport[j].X = vScreen.X / vScreen.W;
                    verticesInViewport[j].Y = vScreen.Y / vScreen.W;
                    verticesInViewport[j].Z = vScreen.Z / vScreen.W;
                    invW[j] = 1.0f / vScreen.W;

                    Vector3 vWorld = MathsOperations.TransformVertex(obj.Vertices[vIndex], modelMatrix);
                    verticesInWorld[j] = vWorld;

                    Vector3 vNormal = MathsOperations.TransformVertex(obj.Normals[nIndex], modelMatrix);
                    vertexNormals[j] = vNormal.Normalize();

                    if (CurrentLightingMode == LightingMode.Texture)
                    {
                        if (obj.textures != null && obj.textures.Length > i * 3 + j)
                        {
                            int tIndex = obj.textures[i * 3 + j] - 1;
                            if (tIndex >= 0 && tIndex < obj.TextureVertices.Count)
                            {
                                var uvw = obj.TextureVertices[tIndex];
                                Vector2 uvCoord = new Vector2(uvw.X, uvw.Y);
                                if (j == 0) uv0 = uvCoord;
                                else if (j == 1) uv1 = uvCoord;
                                else uv2 = uvCoord;
                            }
                            else { if (j == 0) uv0 = Vector2.Zero(); else if (j == 1) uv1 = Vector2.Zero(); else uv2 = Vector2.Zero(); }
                        }
                        else { if (j == 0) uv0 = Vector2.Zero(); else if (j == 1) uv1 = Vector2.Zero(); else uv2 = Vector2.Zero(); }
                    }
                }

                    RasterizeTriangleTexture(bitmap,
                        verticesInViewport[0], verticesInViewport[1], verticesInViewport[2],
                        verticesInWorld[0], verticesInWorld[1], verticesInWorld[2],
                        vertexNormals[0], vertexNormals[1], vertexNormals[2],
                        uv0, uv1, uv2,
                        invW[0], invW[1], invW[2]);
            }
            pictureBox1.Image = bitmap;
        }

        void Barycentric(Vector3 v0, Vector3 v1, Vector3 v2, float x, float y, out float a, out float b, out float c)
        {
            float denom = (v1.Y - v2.Y) * (v0.X - v2.X) + (v2.X - v1.X) * (v0.Y - v2.Y);
            if (Math.Abs(denom) < 1e-6f) denom = 1e-6f;
            a = ((v1.Y - v2.Y) * (x - v2.X) + (v2.X - v1.X) * (y - v2.Y)) / denom;
            b = ((v2.Y - v0.Y) * (x - v2.X) + (v0.X - v2.X) * (y - v2.Y)) / denom;
            c = 1 - a - b;
        }

        private void ClearBitmap(Bitmap bmp)
        {
            BitmapData bmpData = bmp.LockBits(
                new Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;
                int bytes = bmpData.Stride * bmp.Height;
                for (int i = 0; i < bytes; i++) ptr[i] = 255;
            }
            bmp.UnlockBits(bmpData);
        }

        public void RasterizeTriangleTexture(Bitmap bmp,
            Vector3 v0, Vector3 v1, Vector3 v2,
            Vector3 p0, Vector3 p1, Vector3 p2,
            Vector3 n0, Vector3 n1, Vector3 n2,
            Vector2 uv0, Vector2 uv1, Vector2 uv2,
            float invW0, float invW1, float invW2)
        {
            Vector3[] vertices = new Vector3[] { v0, v1, v2 };
            Array.Sort(vertices, (a, b) => a.Y.CompareTo(b.Y));

            Vector3 top = vertices[0];
            Vector3 middle = vertices[1];
            Vector3 bottom = vertices[2];

            int yStart = Math.Max(0, (int)Math.Ceiling(top.Y));
            int yEnd = Math.Min(bmp.Height - 1, (int)Math.Floor(bottom.Y));

            BitmapData bmpData = bmp.LockBits(
                new Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.ReadWrite,
                System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;
                int stride = bmpData.Stride;

                byte* ptrDiffuse = bmpDataDiffuse != null ? (byte*)bmpDataDiffuse.Scan0 : null;
                int strideDiffuse = bmpDataDiffuse != null ? bmpDataDiffuse.Stride : 0;

                byte* ptrNormal = bmpDataNormal != null ? (byte*)bmpDataNormal.Scan0 : null;
                int strideNormal = bmpDataNormal != null ? bmpDataNormal.Stride : 0;

                byte* ptrSpecular = bmpDataSpecular != null ? (byte*)bmpDataSpecular.Scan0 : null;
                int strideSpecular = bmpDataSpecular != null ? bmpDataSpecular.Stride : 0;


                for (int y = yStart; y <= yEnd; y++)
                {
                    float x1 = InterpolateX(y <= middle.Y ? top : middle, y <= middle.Y ? middle : bottom, y);
                    float x2 = InterpolateX(top, bottom, y);

                    int xStart = Math.Max(0, (int)Math.Ceiling(Math.Min(x1, x2)));
                    int xEnd = Math.Min(bmp.Width - 1, (int)Math.Floor(Math.Max(x1, x2)));

                    for (int x = xStart; x <= xEnd; x++)
                    {
                        float z = InterpolateZ(v0, v1, v2, x, y);
                        if (z <= zBuffer[x, y])
                        {
                            zBuffer[x, y] = z;
                            Barycentric(v0, v1, v2, x, y, out float α, out float β, out float γ);

                            float invW = α * invW0 + β * invW1 + γ * invW2;
                            float w = 1.0f / invW;

                            float u = (α * uv0.X * invW0 + β * uv1.X * invW1 + γ * uv2.X * invW2) / invW;
                            float v = (α * uv0.Y * invW0 + β * uv1.Y * invW1 + γ * uv2.Y * invW2) / invW;
                            Vector2 uv = new Vector2(u, v);

                            Vector3 fragPos = (p0 * (α * invW0) + p1 * (β * invW1) + p2 * (γ * invW2)) * w;

                            Vector3 diffuseColor = new Vector3(200, 200, 200); 
                            if (ptrDiffuse != null)
                            {
                                int texX = Math.Clamp((int)(uv.X * diffuseMap.Width), 0, diffuseMap.Width - 1);
                                int texY = Math.Clamp((int)((1 - uv.Y) * diffuseMap.Height), 0, diffuseMap.Height - 1);
                                byte* pixelDiffuse = ptrDiffuse + texY * strideDiffuse + texX * 3;
                                diffuseColor = new Vector3(pixelDiffuse[2], pixelDiffuse[1], pixelDiffuse[0]);
                            }

                            Vector3 mappedNormal;
                            if (ptrNormal != null)
                            {
                                int texX = Math.Clamp((int)(uv.X * normalMap.Width), 0, normalMap.Width - 1);
                                int texY = Math.Clamp((int)((1 - uv.Y) * normalMap.Height), 0, normalMap.Height - 1);
                                byte* pixelNormal = ptrNormal + texY * strideNormal + texX * 3;
                                mappedNormal = new Vector3(
                                    pixelNormal[2] / 255.0f * 2 - 1,
                                    pixelNormal[1] / 255.0f * 2 - 1,
                                    pixelNormal[0] / 255.0f * 2 - 1).Normalize();
                            }
                            else
                            {
                                mappedNormal = (n0 * (α * invW0) + n1 * (β * invW1) + n2 * (γ * invW2) * w).Normalize();
                            }

                            float specularStrength = 0.5f;
                            if (ptrSpecular != null)
                            {
                                int texX = Math.Clamp((int)(uv.X * specularMap.Width), 0, specularMap.Width - 1);
                                int texY = Math.Clamp((int)((1 - uv.Y) * specularMap.Height), 0, specularMap.Height - 1);
                                specularStrength = (ptrSpecular + texY * strideSpecular + texX * 3)[0] / 255.0f;
                            }

                            Vector3 viewDir = (eye - fragPos).Normalize();
                            Vector3 reflectDir = (mappedNormal * 2 * Vector3.ScalarMultiplication(lightDirection, mappedNormal) - lightDirection).Normalize();

                            float NdotL = Math.Max(0, Vector3.ScalarMultiplication(mappedNormal, lightDirection));
                            float specular = (float)Math.Pow(Math.Max(0, Vector3.ScalarMultiplication(viewDir, reflectDir)), 32) * specularStrength;

                            byte* pixel = ptr + y * stride + x * 3;
                            pixel[0] = (byte)Math.Clamp((diffuseColor.Z * NdotL + 255 * specular), 0, 255);
                            pixel[1] = (byte)Math.Clamp((diffuseColor.Y * NdotL + 255 * specular), 0, 255);
                            pixel[2] = (byte)Math.Clamp((diffuseColor.X * NdotL + 255 * specular), 0, 255);
                        }
                    }
                }
            }
            bmp.UnlockBits(bmpData);
        }
        

        private float InterpolateX(Vector3 a, Vector3 b, float y)
        {
            if (Math.Abs(a.Y - b.Y) < 1e-6f) return a.X;
            return a.X + (b.X - a.X) * (y - a.Y) / (b.Y - a.Y);
        }

        private float InterpolateZ(Vector3 v0, Vector3 v1, Vector3 v2, float x, float y)
        {
            float denominator = (v1.Y - v2.Y) * (v0.X - v2.X) + (v2.X - v1.X) * (v0.Y - v2.Y);
            if (Math.Abs(denominator) < 1e-6f) return float.MaxValue;
            float a = ((v1.Y - v2.Y) * (x - v2.X) + (v2.X - v1.X) * (y - v2.Y)) / denominator;
            float b = ((v2.Y - v0.Y) * (x - v2.X) + (v0.X - v2.X) * (y - v2.Y)) / denominator;
            float c = 1 - a - b;
            return a * v0.Z + b * v1.Z + c * v2.Z;
        }

        private void InitializeZBuffer()
        {
            for (int x = 0; x < zBuffer.GetLength(0); x++)
                for (int y = 0; y < zBuffer.GetLength(1); y++)
                    zBuffer[x, y] = float.MaxValue;
        }

        private Vector3 CalculateFaceNormal(Vector3[] triangleVerticesWorld)
        {
            if (triangleVerticesWorld == null || triangleVerticesWorld.Length < 3)
                return Vector3.Zero();

            Vector3 v0 = triangleVerticesWorld[0];
            Vector3 v1 = triangleVerticesWorld[1];
            Vector3 v2 = triangleVerticesWorld[2];

            Vector3 edge1 = v1 - v0;
            Vector3 edge2 = v2 - v0;

            return Vector3.VectorMultiplication(edge1, edge2).Normalize();
        }

        private float CalculateLambertIntensity(Vector3 normal)
        {
            return Math.Min(Math.Max(Vector3.ScalarMultiplication(normal, lightDirection), 0.05f), 1f);
        }
     

        private Vector3 ApplyIntensityToColor(float intensity)
        {
            byte intensityByte = (byte)(255 * intensity);
            return new Vector3(intensityByte, intensityByte, intensityByte);
        }
        public Bitmap LoadTexture(string filePath)
        {
            try { return new Bitmap(filePath); }
            catch { return null; }
        }

        private void HandleKeyPress(Keys key, bool isControlPressed, float deltaTime)
        {
            float actualRotationSpeed = RotationSpeed * deltaTime;
            float actualTranslationSpeed = TranslationSpeed * deltaTime;
            float actualScaleSpeed = RotationSpeed * deltaTime * 0.5f;

            if (isControlPressed)
            {
                switch (key)
                {
                    case Keys.A: lightDirection.X -= actualRotationSpeed; break;
                    case Keys.D: lightDirection.X += actualRotationSpeed; break;
                    case Keys.W: lightDirection.Y += actualRotationSpeed; break;
                    case Keys.S: lightDirection.Y -= actualRotationSpeed; break;
                    case Keys.Q: lightDirection.Z += actualRotationSpeed; break;
                    case Keys.E: lightDirection.Z -= actualRotationSpeed; break;
                }
                lightDirection = lightDirection.Normalize();
            }
            else
            {
                switch (key)
                {
                    case Keys.A: rotationY -= actualRotationSpeed; rotateYMatrix = Matricies.GetRotateYMatrix(rotationY); break;
                    case Keys.D: rotationY += actualRotationSpeed; rotateYMatrix = Matricies.GetRotateYMatrix(rotationY); break;
                    case Keys.W: rotationX -= actualRotationSpeed; rotateXMatrix = Matricies.GetRotateXMatrix(rotationX); break;
                    case Keys.S: rotationX += actualRotationSpeed; rotateXMatrix = Matricies.GetRotateXMatrix(rotationX); break;
                    case Keys.Q: rotationZ -= actualRotationSpeed; rotateZMatrix = Matricies.GetRotateZMatrix(rotationZ); break;
                    case Keys.E: rotationZ += actualRotationSpeed; rotateZMatrix = Matricies.GetRotateZMatrix(rotationZ); break;

                    case Keys.Left: translationX -= actualTranslationSpeed; translationMatrix = Matricies.GetTranslationMatrix(translationX, translationY, translationZ); break;
                    case Keys.Right: translationX += actualTranslationSpeed; translationMatrix = Matricies.GetTranslationMatrix(translationX, translationY, translationZ); break;
                    case Keys.Up: translationY += actualTranslationSpeed; translationMatrix = Matricies.GetTranslationMatrix(translationX, translationY, translationZ); break;
                    case Keys.Down: translationY -= actualTranslationSpeed; translationMatrix = Matricies.GetTranslationMatrix(translationX, translationY, translationZ); break;
                    case Keys.PageUp: translationZ += actualTranslationSpeed; translationMatrix = Matricies.GetTranslationMatrix(translationX, translationY, translationZ); break;
                    case Keys.PageDown: translationZ -= actualTranslationSpeed; translationMatrix = Matricies.GetTranslationMatrix(translationX, translationY, translationZ); break;

                    case Keys.Oemplus: case Keys.Add: scale += actualScaleSpeed; scaleMatrix = Matricies.GetScaleMatrix(scale, scale, scale); break;
                    case Keys.OemMinus: case Keys.Subtract: scale -= actualScaleSpeed; scale = Math.Max(0.1f, scale); scaleMatrix = Matricies.GetScaleMatrix(scale, scale, scale); break;
                }
            }
        }
    }
}