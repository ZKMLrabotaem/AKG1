
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
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

        private Vector3 eye = new Vector3(0, 0, 20);
        private Vector3 target = new Vector3(0, 0, 0);
        private Vector3 up = new Vector3(0, 1, 0);

        // 0.0964412242f, -0.870733261f, 0.482206136f, -1.20551527f
        private Vector3 lightDirection = new Vector3(1, 1, 1).Normalize();

        private const float RotationSpeed = 2f;
        private const float TranslationSpeed = 0.3f;
        private const float MouseWheelSpeed = 0.0005f;
        private System.Windows.Forms.Timer movementTimer;

        private HashSet<Keys> pressedKeys = new HashSet<Keys>();

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
            bitmap = new Bitmap(pictureBox1.Width, pictureBox1.Height, PixelFormat.Format24bppRgb);
            zBuffer = new float[pictureBox1.Width, pictureBox1.Height];
        }

        protected void UpdateScene()
        {
            ClearBitmap(bitmap);
            InitializeZBuffer();

            observerMatrix = Matricies.GetObserverMatrix(eye, target, up);
            projectionMatrix = Matricies.GetPerspectiveProjectionMatrix((float)Math.PI / 2f, (float)bitmap.Width / bitmap.Height, 1f, 1000f);
            viewportMatrix = Matricies.GetViewingWindowMatrix(bitmap.Width, bitmap.Height, 0, 0);

            for (int i = 0; i < objects.Count; i++)
            {
                var obj = objects[i];
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

                    RasterizeTriangleTexture(bitmap,
                        verticesInViewport[0], verticesInViewport[1], verticesInViewport[2],
                        verticesInWorld[0], verticesInWorld[1], verticesInWorld[2],
                        vertexNormals[0], vertexNormals[1], vertexNormals[2],
                        uv0, uv1, uv2,
                        invW[0], invW[1], invW[2],
                        obj.diffuseMap, obj.normalMap, obj.specularMap,
                        obj.bmpDataDiffuse, obj.bmpDataNormal, obj.bmpDataSpecular);
                }
            }

            pictureBox1.Image = bitmap;
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

        public void RasterizeTriangleTexture(Bitmap bmp,
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

                            byte* pixel = ptr + y * stride + x * 3;
                            pixel[0] = (byte)Math.Clamp(result.Z, 0, 255);
                            pixel[1] = (byte)Math.Clamp(result.Y, 0, 255);
                            pixel[2] = (byte)Math.Clamp(result.X, 0, 255);
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
                if (objects.Count > 0)
                {
                    switch (key)
                    {
                        case Keys.A: 
                            objects[0].rotationY -= actualRotationSpeed;
                            /*objects[1].rotationY -= actualRotationSpeed;
                            objects[2].rotationY -= actualRotationSpeed;
                            objects[3].rotationY -= actualRotationSpeed;
                            objects[4].rotationY -= actualRotationSpeed;
                            objects[5].rotationY -= actualRotationSpeed;*/ break;
                        case Keys.D: 
                            objects[0].rotationY += actualRotationSpeed;
                            /*objects[1].rotationY += actualRotationSpeed;
                            objects[2].rotationY += actualRotationSpeed;
                            objects[3].rotationY += actualRotationSpeed;
                            objects[4].rotationY += actualRotationSpeed;
                            objects[5].rotationY += actualRotationSpeed;*/ break;
                        case Keys.W:
                            objects[0].rotationX -= actualRotationSpeed;
                            /*objects[1].rotationX -= actualRotationSpeed;
                            objects[2].rotationX -= actualRotationSpeed;
                            objects[3].rotationX -= actualRotationSpeed;
                            objects[4].rotationX -= actualRotationSpeed;
                            objects[5].rotationX -= actualRotationSpeed;*/ break;
                        case Keys.S: 
                            objects[0].rotationX += actualRotationSpeed;
                            /*objects[1].rotationX += actualRotationSpeed;
                            objects[2].rotationX += actualRotationSpeed;
                            objects[3].rotationX += actualRotationSpeed;
                            objects[4].rotationX += actualRotationSpeed;
                            objects[5].rotationX += actualRotationSpeed;*/ break;
                        case Keys.Q: 
                            objects[0].rotationZ -= actualRotationSpeed;
                            /*objects[1].rotationZ -= actualRotationSpeed;
                            objects[2].rotationZ -= actualRotationSpeed;
                            objects[3].rotationZ -= actualRotationSpeed;
                            objects[4].rotationZ -= actualRotationSpeed;
                            objects[5].rotationZ -= actualRotationSpeed;*/ break;
                        case Keys.E: 
                            objects[0].rotationZ += actualRotationSpeed;
                            /*objects[1].rotationZ += actualRotationSpeed;
                            objects[2].rotationZ += actualRotationSpeed;
                            objects[3].rotationZ += actualRotationSpeed;
                            objects[4].rotationZ += actualRotationSpeed;
                            objects[5].rotationZ += actualRotationSpeed;*/ break;

                        case Keys.Left: objects[0].translationX -= actualTranslationSpeed; break;
                        case Keys.Right: objects[0].translationX += actualTranslationSpeed; break;
                        case Keys.Up: objects[0].translationY += actualTranslationSpeed; break;
                        case Keys.Down: objects[0].translationY -= actualTranslationSpeed; break;
                        case Keys.PageUp: objects[0].translationZ += actualTranslationSpeed; break;
                        case Keys.PageDown: objects[0].translationZ -= actualTranslationSpeed; break;

                        case Keys.Oemplus: case Keys.Add: objects[0].scale += actualScaleSpeed; break;
                        case Keys.OemMinus: case Keys.Subtract: objects[0].scale -= actualScaleSpeed; objects[0].scale = Math.Max(0.1f, objects[0].scale); break;
                    }

                    objects[0].rotateYMatrix = Matricies.GetRotateYMatrix(objects[0].rotationY);
                    /*objects[1].rotateYMatrix = Matricies.GetRotateYMatrix(objects[1].rotationY);
                    objects[2].rotateYMatrix = Matricies.GetRotateYMatrix(objects[2].rotationY);
                    objects[3].rotateYMatrix = Matricies.GetRotateYMatrix(objects[2].rotationY);
                    objects[4].rotateYMatrix = Matricies.GetRotateYMatrix(objects[2].rotationY);
                    objects[5].rotateYMatrix = Matricies.GetRotateYMatrix(objects[2].rotationY);*/
                    objects[0].rotateXMatrix = Matricies.GetRotateXMatrix(objects[0].rotationX);
                    /*objects[1].rotateXMatrix = Matricies.GetRotateXMatrix(objects[1].rotationX);
                    objects[2].rotateXMatrix = Matricies.GetRotateXMatrix(objects[2].rotationX);
                    objects[3].rotateXMatrix = Matricies.GetRotateXMatrix(objects[2].rotationX);
                    objects[4].rotateXMatrix = Matricies.GetRotateXMatrix(objects[2].rotationX);
                    objects[5].rotateXMatrix = Matricies.GetRotateXMatrix(objects[2].rotationX);*/
                    objects[0].rotateZMatrix = Matricies.GetRotateZMatrix(objects[0].rotationZ);
                    /*objects[1].rotateZMatrix = Matricies.GetRotateZMatrix(objects[1].rotationZ);
                    objects[2].rotateZMatrix = Matricies.GetRotateZMatrix(objects[2].rotationZ);
                    objects[3].rotateZMatrix = Matricies.GetRotateZMatrix(objects[2].rotationZ);
                    objects[4].rotateZMatrix = Matricies.GetRotateZMatrix(objects[2].rotationZ);
                    objects[5].rotateZMatrix = Matricies.GetRotateZMatrix(objects[2].rotationZ);*/
                    objects[0].scaleMatrix = Matricies.GetScaleMatrix(objects[0].scale, objects[0].scale, objects[0].scale);
                    objects[0].translationMatrix = Matricies.GetTranslationMatrix(objects[0].translationX, objects[0].translationY, objects[0].translationZ);
                }
            }
        }
    }
}
