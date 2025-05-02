
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Media;
using Lab_1.ParseObject;
using Lab_1.Structures;
using lab1.MatrixOperations;

using lab1.Structures;

namespace lab1.Forms
{
    public partial class Scene : Form
    {
        private Dictionary<string, string> objectsPaths = new Dictionary<string, string>
        {
            { "bass", "Objects\\bass_object.obj" },
            { "algae1", "Objects\\algae_object.obj" },
            { "algae2", "Objects\\algae_object.obj" },
            { "algae3", "Objects\\algae_object.obj" },
            { "algae4", "Objects\\algae_object.obj" },
            { "sand", "Objects\\sand_object.obj" },
        };
        private List<BaseObject> objects = new List<BaseObject>();
        public float[,] modelMatrix, observerMatrix, projectionMatrix, viewportMatrix, resultMatrix;

        private Vector3 eye = new Vector3(0, 1, -3);
        //private Vector3 eye = new Vector3(0, 0, 2);
        //private Vector3 eye = new Vector3(2, 0, 0.5f);
        private Vector3 target = new Vector3(0, 0, 0);
        private Vector3 up = new Vector3(0, 1, 0);

        // 0.0964412242f, -0.870733261f, 0.482206136f, -1.20551527f
        private Vector3 lightDirection = new Vector3(1, 1, 1).Normalize();

        private const float RotationSpeed = 2f;
        private const float TranslationSpeed = 0.3f;
        private const float MouseWheelSpeed = 0.0005f;
        private System.Windows.Forms.Timer movementTimer;

        private HashSet<Keys> pressedKeys = new HashSet<Keys>();
        private Random random = new Random();
       private float[,] zBuffer;
        private Bitmap bitmap;

        private readonly Vector3[] verticesInViewport = new Vector3[3];
        private readonly Vector3[] verticesInWorld = new Vector3[3];
        private readonly Vector3[] vertexNormals = new Vector3[3];
        private float[] invW = new float[3];
        private Vector2 uv0 = default, uv1 = default, uv2 = default;

        private DateTime lastFrameTime = DateTime.Now;
        private DateTime lastAnimationUpdateTime = DateTime.Now;
        private float deltaTime = 0;

        private float nearPlaneDistance = 1.0f;
        struct PixelData
        {
            public byte R, G, B;
        }

        private float mouseSensitivity = 0.002f;
        private float movementSpeed = 5f;
        private float cameraPitch = 0f; // Угол наклона камеры вверх/вниз
        private float cameraYaw = 0f;   // Угол поворота камеры влево/вправо

        private Vector3 playerPosition = Vector3.Zero(); // Примерная позиция
        private Vector3 playerForward = Vector3.UnitZ; // Начальное направление (вперёд)

        // Расстояние от камеры до объекта (третье лицо)
        private float cameraDistance = 5f;

        private bool isMouseCaptured = false;
        private Point lastMousePosition;

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
          
            foreach (var path in objectsPaths)
            {
                switch (path.Key)
                {
                    case "bass": objects.Add(new Bass(path.Value)); break;
                    case "algae1": 
                        objects.Add(new Algae(path.Value));
                        (objects.LastOrDefault() as Algae).ChangeModelMatrix(-7.3f, -8f, -7.5f);
                        break;
                    case "algae2":
                        objects.Add(new Algae(path.Value));
                        (objects.LastOrDefault() as Algae).ChangeModelMatrix(7.3f, -8.8f, -7.5f);
                        break;
                    case "algae3":
                        objects.Add(new Algae(path.Value));
                        (objects.LastOrDefault() as Algae).ChangeModelMatrix(7.3f, -7.5f, 7.5f);
                        break;
                    case "algae4":
                        objects.Add(new Algae(path.Value));
                        (objects.LastOrDefault() as Algae).ChangeModelMatrix(-7.3f, -6.6f, 7.5f);
                        break;
                    case "sand": objects.Add(new Sand(path.Value)); break;
                }
            }
            this.KeyDown += Scene_KeyDown;
            this.KeyUp += Scene_KeyUp;
            movementTimer = new System.Windows.Forms.Timer
            {
                Interval = 8
            };
            movementTimer.Tick += MovementTimer_Tick;
            movementTimer.Start();

            this.Capture = true; 

            this.MouseMove += OnMouseMove;

            Cursor.Hide();
            Cursor.Position = new Point(
                this.Left + this.Width / 2,
                this.Top + this.Height / 2
            );

            CreateBitmap();
            UpdateScene();
        }

        private void Scene_KeyDown(object sender, KeyEventArgs e)
        {
            pressedKeys.Add(e.KeyCode);
        }

        private void Scene_KeyUp(object sender, KeyEventArgs e)
        {
            pressedKeys.Remove(e.KeyCode);
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
        }
        private void MovementTimer_Tick(object sender, EventArgs e)
        {
            DateTime currentFrameTime = DateTime.Now;
            deltaTime = (float)(currentFrameTime - lastFrameTime).TotalSeconds;
            lastFrameTime = currentFrameTime;

            label1.Text = "FPS: " + float.Ceiling(1 / deltaTime);

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
            bitmap = new Bitmap(pictureBox1.Width, pictureBox1.Height+20, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            zBuffer = new float[pictureBox1.Width, pictureBox1.Height+20];
        }

        protected void UpdateScene()
        {
            ClearBitmap(bitmap);
            InitializeZBuffer();

            observerMatrix = Matricies.GetObserverMatrix(eye, target, up);
            projectionMatrix = Matricies.GetPerspectiveProjectionMatrix((float)Math.PI / 2f, (float)bitmap.Width / bitmap.Height, 1f, 1000f);
            viewportMatrix = Matricies.GetViewingWindowMatrix(bitmap.Width, bitmap.Height, 0, 0);
            int width = bitmap.Width;
            int height = bitmap.Height;

            var buffers = new RenderBuffer[6];
            for (int i = 0; i < 6; i++)
                buffers[i] = new RenderBuffer(width, height);

            var tasks = new List<Task>();

            int countPerThread = (int)Math.Ceiling(objects.Count / 6.0);

            for (int i = 0; i < 6; i++)
            {
                int start = i * countPerThread;
                int end = Math.Min(start + countPerThread, objects.Count);
                int index = i;

                tasks.Add(Task.Run(() =>
                {
                    for (int j = start; j < end; j++)
                        DrawObject(objects[j], buffers[index]);
                }));
            }

            Task.WhenAll(tasks).Wait();

            AssembleBitmap(buffers);
            pictureBox1.Image = bitmap;
        }
        private void DrawObject(BaseObject obj, RenderBuffer buffer)
        {

            
            float[,] modelMatrix = obj.GetModelMatrix();
            float[,] modelMatrixInverseTranspose = MathsOperations.InverseTransposeMatrix(modelMatrix);

            int faceCount = obj.objectModel.faces.Length / 3;
            var currentVerticies = obj.GetCurrentVertices();

            resultMatrix = MathsOperations.MultipleMatrix(
                MathsOperations.MultipleMatrix(
                    MathsOperations.MultipleMatrix(
                        viewportMatrix,
                        projectionMatrix),
                    observerMatrix),
                modelMatrix);

            for (int j = 0; j < faceCount; j++)
            {
                Vector3[] verticesInView = new Vector3[3];
                for (int k = 0; k < 3; k++)
                {
                    int vIndex = obj.objectModel.faces[j * 3 + k] - 1;
                    int nIndex = obj.objectModel.normals[j * 3 + k] - 1;

                    Vector3 vScreen = MathsOperations.TransformVertex(currentVerticies[vIndex], resultMatrix);
                    if (vScreen.W == 0) vScreen.W = 1e-6f;

                    verticesInViewport[k].X = vScreen.X / vScreen.W;
                    verticesInViewport[k].Y = vScreen.Y / vScreen.W;
                    verticesInViewport[k].Z = vScreen.Z / vScreen.W;
                    invW[k] = 1.0f / vScreen.W;

                    Vector3 vWorld = MathsOperations.TransformVertex(obj.objectModel.Vertices[vIndex], modelMatrix);
                    verticesInWorld[k] = vWorld;

                    Vector3 originalNormal = obj.objectModel.Normals[nIndex % obj.objectModel.Normals.Count];
                    Vector3 vNormal = MathsOperations.TransformVertex(originalNormal, modelMatrixInverseTranspose);
                    vertexNormals[k] = vNormal.Normalize();

                    verticesInView[k] = MathsOperations.TransformVertex(vWorld, observerMatrix);

                    if (CurrentLightingMode == LightingMode.Texture)
                    {
                        if (obj.objectModel.textures != null && obj.objectModel.textures.Length > j * 3 + k)
                        {
                            int tIndex = obj.objectModel.textures[j * 3 + k] - 1;
                            if (tIndex >= 0 && tIndex < obj.objectModel.TextureVertices.Count)
                            {
                                var uvw = obj.objectModel.TextureVertices[tIndex];
                                Vector2 uvCoord = new Vector2(uvw.X, uvw.Y);
                                if (k == 0) uv0 = uvCoord;
                                else if (k == 1) uv1 = uvCoord;
                                else uv2 = uvCoord;
                            }
                            else { if (k == 0) uv0 = Vector2.Zero(); else if (k == 1) uv1 = Vector2.Zero(); else uv2 = Vector2.Zero(); }
                        }
                        else { if (k == 0) uv0 = Vector2.Zero(); else if (k == 1) uv1 = Vector2.Zero(); else uv2 = Vector2.Zero(); }
                    }
                }

                if (verticesInView[0].Z > -nearPlaneDistance &&
                    verticesInView[1].Z > -nearPlaneDistance &&
                    verticesInView[2].Z > -nearPlaneDistance)
                    continue;

                RasterizeTriangleTexture(buffer,
                    verticesInViewport[0], verticesInViewport[1], verticesInViewport[2],
                    verticesInWorld[0], verticesInWorld[1], verticesInWorld[2],
                    vertexNormals[0], vertexNormals[1], vertexNormals[2],
                    uv0, uv1, uv2,
                    invW[0], invW[1], invW[2],
                    obj.diffuseMap, obj.normalMap, obj.specularMap,
                    obj.bmpDataDiffuse, obj.bmpDataNormal, obj.bmpDataSpecular);
            }
        }
        private void AssembleBitmap(RenderBuffer[] buffers)
        {
            BitmapData data = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            int stride = data.Stride;
            int width = bitmap.Width;
            int height = bitmap.Height;

            unsafe
            {
                byte* destPtr = (byte*)data.Scan0;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float minZ = float.MaxValue;
                        byte r = 0, g = 0, b = 0;

                        for (int i = 0; i < buffers.Length; i++)
                        {
                            float z = buffers[i].ZBuffer[x, y];
                            if (z < minZ)
                            {
                                minZ = z;
                                int index = (y * width + x) * 3;
                                b = buffers[i].Pixels[index];
                                g = buffers[i].Pixels[index + 1];
                                r = buffers[i].Pixels[index + 2];
                            }
                        }

                        int destIndex = y * stride + x * 3;
                        destPtr[destIndex] = b;
                        destPtr[destIndex + 1] = g;
                        destPtr[destIndex + 2] = r;
                    }
                }
            }

            bitmap.UnlockBits(data);
        }

           


        private void Barycentric(Vector3 v0, Vector3 v1, Vector3 v2, float x, float y, out float a, out float b, out float c)
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

        public void RasterizeTriangleTexture( RenderBuffer buffer,
            Vector3 v0, Vector3 v1, Vector3 v2,
            Vector3 p0, Vector3 p1, Vector3 p2,
            Vector3 n0, Vector3 n1, Vector3 n2,
            Vector2 uv0, Vector2 uv1, Vector2 uv2,
            float invW0, float invW1, float invW2,
            Bitmap diffuseMap, Bitmap normalMap, Bitmap specularMap,
            BitmapData bmpDataDiffuse, BitmapData bmpDataNormal, BitmapData bmpDataSpecular)
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

                            float u = (α * uv0.X * invW0 + β * uv1.X * invW1 + γ * uv2.X * invW2) * w;
                            float v = (α * uv0.Y * invW0 + β * uv1.Y * invW1 + γ * uv2.Y * invW2) * w;
                            Vector2 uv = new Vector2(u, v);

                            Vector3 fragPos = (p0 * (α * invW0) + p1 * (β * invW1) + p2 * (γ * invW2)) * w;
                            Vector3 normal = (n0 * (α * invW0) + n1 * (β * invW1) + n2 * (γ * invW2)) * w;
                            normal = normal.Normalize();

                            Vector3 diffuseColor = new Vector3(200, 200, 200);
                            if (ptrDiffuse != null)
                            {
                                int texX = Math.Clamp((int)(uv.X * diffuseMap.Width), 0, diffuseMap.Width - 1);
                                int texY = Math.Clamp((int)((1 - uv.Y) * diffuseMap.Height), 0, diffuseMap.Height - 1);
                                byte* pixelDiffuse = ptrDiffuse + texY * strideDiffuse + texX * 3;
                                diffuseColor = new Vector3(pixelDiffuse[2], pixelDiffuse[1], pixelDiffuse[0]);
                            }

                            Vector3 finalNormal;
                            if (ptrNormal != null)
                            {
                                int texX = Math.Clamp((int)(uv.X * normalMap.Width), 0, normalMap.Width - 1);
                                int texY = Math.Clamp((int)((1 - uv.Y) * normalMap.Height), 0, normalMap.Height - 1);
                                byte* pixelNormal = ptrNormal + texY * strideNormal + texX * 3;

                                Vector3 tangentNormal = new Vector3(
                                    pixelNormal[2] / 255.0f * 2 - 1,
                                    pixelNormal[1] / 255.0f * 2 - 1,
                                    pixelNormal[0] / 255.0f * 2 - 1).Normalize();

                                Vector3 edge1 = p1 - p0;
                                Vector3 edge2 = p2 - p0;
                                Vector2 deltaUV1 = uv1 - uv0;
                                Vector2 deltaUV2 = uv2 - uv0;

                                float f = 1.0f / (deltaUV1.X * deltaUV2.Y - deltaUV2.X * deltaUV1.Y);

                                Vector3 tangent = (edge1 * deltaUV2.Y - edge2 * deltaUV1.Y) * f;
                                tangent = tangent.Normalize();
                                Vector3 bitangent = (edge2 * deltaUV1.X - edge1 * deltaUV2.X) * f;
                                bitangent = bitangent.Normalize();

                                Matrix3x3 TBN = new Matrix3x3(
                                    tangent.X, bitangent.X, normal.X,
                                    tangent.Y, bitangent.Y, normal.Y,
                                    tangent.Z, bitangent.Z, normal.Z);

                                finalNormal = TBN.Transform(tangentNormal).Normalize();
                            }
                            else
                            {
                                finalNormal = normal;
                            }

                            float specularStrength = 0.5f;
                            if (ptrSpecular != null)
                            {
                                int texX = Math.Clamp((int)(uv.X * specularMap.Width), 0, specularMap.Width - 1);
                                int texY = Math.Clamp((int)((1 - uv.Y) * specularMap.Height), 0, specularMap.Height - 1);
                                specularStrength = (ptrSpecular + texY * strideSpecular + texX * 3)[0] / 255.0f;
                            }

                            Vector3 viewDir = (eye - fragPos).Normalize();
                            Vector3 lightDir = lightDirection.Normalize();

                            float NdotL = Math.Max(0, Vector3.ScalarMultiplication(finalNormal, lightDir));
                            Vector3 diffuse = diffuseColor * NdotL;

                            Vector3 halfwayDir = (lightDir + viewDir).Normalize();
                            float spec = (float)Math.Pow(Math.Max(0, Vector3.ScalarMultiplication(finalNormal, halfwayDir)), 32) * specularStrength;
                            Vector3 specular = new Vector3(255, 255, 255) * spec;

                            Vector3 result = diffuse + specular;

                            result.X *= 0.5f; 
                            result.Y *= 0.9f; 
                            result.Z = Math.Min(255, result.Z * 1f + 30); 

                            int waveAmplitude = 5;
                            float waveFrequency = 0.05f;
                          // int distortedX = x + (int)(Math.Sin(y * waveFrequency + deltaTime) * waveAmplitude + deltaTime );
                            int distortedY = y + (int)(Math.Cos(x * waveFrequency + deltaTime *2) * waveAmplitude + deltaTime * 2);


                            if (/*distortedX >= 0 && distortedX < bmp.Width &&*/ distortedY >= 0 && distortedY < (bmp.Height))
                            {
                                byte* pixel = ptr + distortedY * stride + x * 3;
                                pixel[0] = (byte)Math.Clamp(result.Z, 0, 255);
                                pixel[1] = (byte)Math.Clamp(result.Y, 0, 255);
                                pixel[2] = (byte)Math.Clamp(result.X, 0, 255);
                            }


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


        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            int deltaX = e.X - lastMousePosition.X;
            int deltaY = e.Y - lastMousePosition.Y;

            // Вращение камеры по горизонтали
            cameraYaw -= deltaX * mouseSensitivity;

            // Вращение камеры по вертикали
            cameraPitch -= deltaY * mouseSensitivity;
            cameraPitch = Math.Clamp(cameraPitch, -MathF.PI / 2 + 0.1f, MathF.PI / 2 - 0.1f);

            // Обновляем направление персонажа
            playerForward = new Vector3(MathF.Sin(cameraYaw), 0f, MathF.Cos(cameraYaw)).Normalize();

            // Поворачиваем объект вместе с камерой
            if (objects.Count > 0)
            {
                objects[0].rotationY = cameraYaw;
                UpdateObjectMatrices(); // Добавляем вызов обновления матриц
            }

            UpdateCameraPosition();

            lastMousePosition = new Point(Width / 2, Height / 2);
            Cursor.Position = PointToScreen(lastMousePosition);
        }

        // Новый метод для обновления матриц объекта
        private void UpdateObjectMatrices()
        {
            if (objects.Count == 0) return;

            var obj = objects[0];

            // Порядок важен! Сначала поворот, потом перемещение
            obj.rotateYMatrix = Matricies.GetRotateYMatrix(obj.rotationY);
            obj.rotateXMatrix = Matricies.GetRotateXMatrix(obj.rotationX);
            obj.rotateZMatrix = Matricies.GetRotateZMatrix(obj.rotationZ);
            obj.scaleMatrix = Matricies.GetScaleMatrix(obj.scale, obj.scale, obj.scale);
            obj.translationMatrix = Matricies.GetTranslationMatrix(
                obj.translationX,
                obj.translationY,
                obj.translationZ);
        }

        private void HandleKeyPress(Keys key, bool isControlPressed, float deltaTime)
        {
            float actualMovementSpeed = movementSpeed * deltaTime;

            // Получаем направление камеры (уже нормализовано в UpdateCameraPosition)
            Vector3 cameraForward = (target - eye).Normalize();
            Vector3 cameraRight = Vector3.VectorMultiplication(cameraForward, up).Normalize();

            // Движение только по горизонтали
            Vector3 horizontalForward = new Vector3(cameraForward.X, 0, cameraForward.Z).Normalize();
            Vector3 horizontalRight = new Vector3(cameraRight.X, 0, cameraRight.Z).Normalize();

            switch (key)
            {
                case Keys.W:
                    MoveObject(horizontalForward * actualMovementSpeed);
                    break;
                case Keys.S:
                    MoveObject(-horizontalForward * actualMovementSpeed);
                    break;
                case Keys.A:
                    MoveObject(-horizontalRight * actualMovementSpeed);
                    break;
                case Keys.D:
                    MoveObject(horizontalRight * actualMovementSpeed);
                    break;
                case Keys.Space:
                    MoveObject(up * actualMovementSpeed);
                    break;
                case Keys.C:
                    MoveObject(-up * actualMovementSpeed);
                    break;
            }
        }

        private void MoveObject(Vector3 movement)
        {
            playerPosition += movement;

            if (objects.Count > 0)
            {
                float factor = 1 / objects[0].scale;
                objects[0].translationX += movement.X * factor;
                objects[0].translationY += movement.Y * factor;
                objects[0].translationZ += movement.Z * factor;

                UpdateObjectMatrices(); // Обновляем матрицы после перемещения
            }

            UpdateCameraPosition();
        }

        private void UpdateCameraPosition()
        {
            // Вычисляем позицию камеры за спиной персонажа
            Vector3 cameraOffset = new Vector3(
                MathF.Cos(cameraPitch) * MathF.Sin(cameraYaw),
                MathF.Sin(cameraPitch),
                MathF.Cos(cameraPitch) * MathF.Cos(cameraYaw)
            ) * cameraDistance;

            eye = playerPosition - cameraOffset;
            target = playerPosition; // Камера всегда смотрит на персонажа

            // Обновляем вектор "вверх" камеры, чтобы она не переворачивалась
            up = new Vector3(0, 1, 0);
        }
    }
    public class RenderBuffer
    {
        public byte[] Pixels;
        public float[,] ZBuffer;

        public RenderBuffer(int width, int height)
        {
            Pixels = new byte[width * height * 3]; // 3 канала: B, G, R
            ZBuffer = new float[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    ZBuffer[x, y] = float.MaxValue;
        }

        public void Clear()
        {
            Array.Clear(Pixels, 0, Pixels.Length);
            for (int x = 0; x < ZBuffer.GetLength(0); x++)
                for (int y = 0; y < ZBuffer.GetLength(1); y++)
                    ZBuffer[x, y] = float.MaxValue;
        }
    }


}
