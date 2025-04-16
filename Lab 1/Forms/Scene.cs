using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.DirectoryServices.ActiveDirectory;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Aspose.ThreeD.Render;
using lab1.MatrixOperations;
using lab1.ParseObject;
using lab1.Structures;
using static System.Windows.Forms.AxHost;

namespace lab1.Forms
{
    public partial class Scene : Form
    {
        private ObjModel obj;
        private float rotationX = 0;
        private float rotationY = 0;
        private float rotationZ = 0;
        private float scale = 1f;
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

        private const float rotationSpeed = 0.1f;
        private const float translationSpeed = 0.3f;
        private const float speed = 0.005f;
        private System.Windows.Forms.Timer movementTimer;

        private const string city = "Objects\\Castelia City.obj";
        private const string texturePath = "Objects\\Castelia City.obj";
        private const string head = "Objects\\scull.obj";
        private const string plant = "Objects\\plant.obj";
        private const string cooler = "Objects\\cooler.obj";
        private const string shark = "Objects\\shark.obj";
        private const string car = "Objects\\car.obj";
        private const string eyeball = "Objects\\eyeball.obj";
        private const string church = "Objects\\church_of_st._tiss.obj";
        private HashSet<Keys> pressedKeys = new HashSet<Keys>();

        private float[,] zBuffer;

        Bitmap bitmap;

        Vector3[] verticesInViewport = new Vector3[3];
        Vector3[] verticecInWorld = new Vector3[3];
        Vector3[] verticiesNormals = new Vector3[3];
        Vector3 v, u, n;
        Vector3 normal;
        Vector3 color;
        int vIndex, nIndex, tIndex;
        float intensity;
        private int objectMode = 1;
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
            obj = new ObjModel(plant);
            this.KeyDown += Scene_KeyDown;
            this.KeyUp += Scene_KeyUp;
            movementTimer = new System.Windows.Forms.Timer();
            movementTimer.Interval = 8;
            movementTimer.Tick += MovementTimer_Tick;
            rotateXMatrix = Matricies.GetRotateXMatrix(rotationX);
            rotateYMatrix = Matricies.GetRotateYMatrix(rotationY);
            rotateZMatrix = Matricies.GetRotateZMatrix(rotationZ);
            scaleMatrix = Matricies.GetScaleMatrix(scale, scale, scale);
            translationMatrix = Matricies.GetTranslationMatrix(translationX, translationY, translationZ);
            lightDirection = lightDirection * -1;
            diffuseMap = LoadTexture("Objects\\D.jpg");
            normalMap = LoadTexture("Objects\\N.jpg");
            specularMap = LoadTexture("Objects\\REF.jpg");
            update();
        }
        private void Scene_KeyDown(object sender, KeyEventArgs e)
        {
            if (!pressedKeys.Contains(e.KeyCode))
            {
                pressedKeys.Add(e.KeyCode);
            }

            if (!movementTimer.Enabled)
            {
                movementTimer.Start();
            }
            if (e.KeyCode == Keys.L)
            {
                CurrentLightingMode = CurrentLightingMode == LightingMode.Lambert ? LightingMode.Phong : LightingMode.Lambert;
                update();
            }
            if (e.KeyCode == Keys.T)
            {
                CurrentLightingMode = CurrentLightingMode == LightingMode.Lambert ? LightingMode.Texture : LightingMode.Lambert;
                update();
            }

        }
      
        private void Scene_KeyUp(object sender, KeyEventArgs e)
        {
            if (pressedKeys.Contains(e.KeyCode))
            {
                pressedKeys.Remove(e.KeyCode);
            }

            if (pressedKeys.Count == 0)
            {
                movementTimer.Stop();
            }
        }

        private void MovementTimer_Tick(object sender, EventArgs e)
        {
            bool isCtrlPressed = (Control.ModifierKeys & Keys.Control) == Keys.Control;
            foreach (var key in pressedKeys)
            {
                HandleKeyPress(key, isCtrlPressed);
            }
            update();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            var moveDirection = new Vector3(0, 0, 0);
            var scrollWheel = e.Delta;
            if (ModifierKeys.HasFlag(Keys.Alt))
            {
                eye.X += scrollWheel * speed / 15;
                target.X += scrollWheel * speed / 15;
                scrollWheel *= -1;
            }
            else if (ModifierKeys.HasFlag(Keys.Control))
            {
                eye.Y += scrollWheel * speed / 15;
                target.Y = Math.Clamp(eye.Y, 0.1f, float.PositiveInfinity);
            }
            else
            {
                scrollWheel *= -1;
                eye.Z += scrollWheel * speed;
                target.Z += scrollWheel * speed;
            }
            Vector3 moveOffset = moveDirection * speed * scrollWheel;
            eye += moveOffset;
            update();
        }

        protected void update()
        {
            bitmap = new Bitmap(pictureBox1.Width, pictureBox1.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            zBuffer = new float[pictureBox1.Width, pictureBox1.Height];
            InitializeZBuffer();

            // Model
            // rotateZMatrx * rotateYMatrix * rotateXMatrix * scaleMatrix * translationMatrix
            modelMatrix = MathsOperations.MultipleMatrix(
                MathsOperations.MultipleMatrix(
                    MathsOperations.MultipleMatrix(
                        MathsOperations.MultipleMatrix(
                            rotateZMatrix,
                            rotateYMatrix
                            ),
                        rotateXMatrix
                        ),
                    scaleMatrix
                    ),
                translationMatrix
                );

            // View
            observerMatrix = Matricies.GetObserverMatrix(eye, target, up);

            // Projection
            projectionMatrix = Matricies.GetPerspectiveProjectionMatrix(float.Pi / 2f, bitmap.Width / (float)bitmap.Height, 1, 1000);

            // Viewport
            viewportMatrix = Matricies.GetViewingWindowMatrix(bitmap.Width, (float)bitmap.Height, 0, 0);

            resultMatrix = MathsOperations.MultipleMatrix(
                MathsOperations.MultipleMatrix(
                    MathsOperations.MultipleMatrix(
                        viewportMatrix,
                        projectionMatrix
                        ),
                    observerMatrix
                    ),
                modelMatrix
                );


            int count = obj.faces.Length / 3;
            Vector2 uv0 = default, uv1 = default, uv2 = default;
            for (int i = 0; i < count; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    vIndex = obj.faces[i * 3 + j] - 1;
                    nIndex = obj.normals[i * 3 + j] - 1;

                    v = MathsOperations.TransformVertex(obj.Vertices[vIndex], resultMatrix);

                    verticesInViewport[j].X = v.X / v.W;
                    verticesInViewport[j].Y = v.Y / v.W;
                    verticesInViewport[j].Z = v.Z / v.W;

                    u = MathsOperations.TransformVertex(obj.Vertices[vIndex], modelMatrix);
                    verticecInWorld[j].X = u.X;
                    verticecInWorld[j].Y = u.Y;
                    verticecInWorld[j].Z = u.Z;


                    n = MathsOperations.TransformVertex(obj.Normals[nIndex], modelMatrix);
                    verticiesNormals[j].X = n.X;
                    verticiesNormals[j].Y = n.Y;
                    verticiesNormals[j].Z = n.Z;



                    if (CurrentLightingMode == LightingMode.Texture)
                    {
                        tIndex = obj.textures[i * 3 + j] - 1;
                        var uv = obj.TextureVertices[tIndex];
                        Vector2 uvCoord = new Vector2(uv.X, uv.Y);
                        if (j == 0) uv0 = uvCoord;
                        if (j == 1) uv1 = uvCoord;
                        if (j == 2) uv2 = uvCoord;
                    }
                   
                }
          
                normal = CalculateFaceNormal(verticecInWorld);

                //if (Vector3.ScalarMultiplication(normal.Normalize(), (target.Normalize() - eye.Normalize()).Normalize()) > 0) continue;

                if (CurrentLightingMode == LightingMode.Lambert)
                {
                    intensity = CalculateLambertIntensity(normal);
                    color = ApplyIntensityToColor(intensity);
                    RasterizeTriangle(bitmap, verticesInViewport[0], verticesInViewport[1], verticesInViewport[2], color);
                }
                else if (CurrentLightingMode == LightingMode.Phong)
                {
                    intensity = CalculatePhongIntensity(normal, verticecInWorld[0]);
                    RasterizeTriangle(bitmap, verticesInViewport[0], verticesInViewport[1], verticesInViewport[2],
                    verticiesNormals[0], verticiesNormals[1], verticiesNormals[2],
                    verticecInWorld[0], verticecInWorld[1], verticecInWorld[2]);
                }
                else if (CurrentLightingMode == LightingMode.Texture)
                {
                    RasterizeTriangleTexture(bitmap,
       verticesInViewport[0], verticesInViewport[1], verticesInViewport[2],
       verticecInWorld[0], verticecInWorld[1], verticecInWorld[2],
       verticiesNormals[0], verticiesNormals[1], verticiesNormals[2],
       uv0, uv1, uv2,
       diffuseMap, normalMap, specularMap);
                }

            }

            pictureBox1.Image = bitmap;
        }

        Vector3 InterpolateNormal(Vector3 n0, Vector3 n1, Vector3 n2, float a, float b, float c)
        {
            return (n0 * a + n1 * b + n2 * c).Normalize();
        }

        Vector3 InterpolatePosition(Vector3 p0, Vector3 p1, Vector3 p2, float a, float b, float c)
        {
            return p0 * a + p1 * b + p2 * c;
        }
        Vector2 InterpolateUV(Vector2 uv0, Vector2 uv1, Vector2 uv2, float a, float b, float c)
        {
            return new Vector2(
                a * uv0.X + b * uv1.X + c * uv2.X,
                a * uv0.Y + b * uv1.Y + c * uv2.Y
            );
        }

        Color SampleTexture(Bitmap texture, Vector2 uv)
        {
            int x = Math.Clamp((int)(uv.X * texture.Width), 0, texture.Width - 1);
            int y = Math.Clamp((int)((1 - uv.Y) * texture.Height), 0, texture.Height - 1);
            return texture.GetPixel(x, y);
        }

        Vector3 SampleNormalMap(Bitmap normalMap, Vector2 uv)
        {
            Color c = SampleTexture(normalMap, uv);
            float nx = c.R / 255.0f * 2 - 1;
            float ny = c.G / 255.0f * 2 - 1;
            float nz = c.B / 255.0f * 2 - 1;
            return new Vector3(nx, ny, nz);
        }

        float SampleSpecularMap(Bitmap specularMap, Vector2 uv)
        {
            return SampleTexture(specularMap, uv).R / 255.0f;
        }

        void Barycentric(Vector3 v0, Vector3 v1, Vector3 v2, float x, float y, out float a, out float b, out float c)
        {
            float denom = (v1.Y - v2.Y) * (v0.X - v2.X) + (v2.X - v1.X) * (v0.Y - v2.Y);
            a = ((v1.Y - v2.Y) * (x - v2.X) + (v2.X - v1.X) * (y - v2.Y)) / denom;
            b = ((v2.Y - v0.Y) * (x - v2.X) + (v0.X - v2.X) * (y - v2.Y)) / denom;
            c = 1 - a - b;
        }

        private void ClearBitmap(Bitmap bitmap)
        {
            BitmapData bmpData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;
                int bytes = bmpData.Stride * bitmap.Height;

                for (int i = 0; i < bytes; i++)
                    ptr[i] = 200;
            }

            bitmap.UnlockBits(bmpData);
        }

        private void DrawLine(Bitmap bitmap, Vector3 p1, Vector3 p2, Vector3 clr)
        {
            BitmapData bmpData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadWrite,
                System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;
                int stride = bmpData.Stride;
                int x1 = (int)p1.X, y1 = (int)p1.Y;
                int x2 = (int)p2.X, y2 = (int)p2.Y;
                int dx = Math.Abs(x2 - x1);
                int dy = Math.Abs(y2 - y1);
                int sx = (x1 < x2) ? 1 : -1;
                int sy = (y1 < y2) ? 1 : -1;
                int err = dx - dy;

                while (true)
                {
                    if (x1 >= 0 && x1 < bitmap.Width && y1 >= 0 && y1 < bitmap.Height)
                    {
                        byte* pixel = ptr + y1 * stride + x1 * 3;
                        pixel[0] = (byte)clr.Z;
                        pixel[1] = (byte)clr.Y;
                        pixel[2] = (byte)clr.X;
                    }

                    if (x1 == x2 && y1 == y2) break;

                    int err2 = err * 2;
                    if (err2 > -dy) { err -= dy; x1 += sx; }
                    if (err2 < dx) { err += dx; y1 += sy; }
                }
            }

            bitmap.UnlockBits(bmpData);
        }

        private void DrawPixel(Bitmap bitmap, int x, int y, Vector3 clr)
        {

            BitmapData bmpData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadWrite,
                System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;
                int stride = bmpData.Stride;

                byte* pixel = ptr + y * stride + x * 3;
                pixel[0] = (byte)clr.Z;
                pixel[1] = (byte)clr.Y;
                pixel[2] = (byte)clr.X;
            }

            bitmap.UnlockBits(bmpData);
        }
        public void RasterizeTriangleTexture(Bitmap bitmap, Vector3 v0, Vector3 v1, Vector3 v2,
            Vector3 p0, Vector3 p1, Vector3 p2,
            Vector3 n0, Vector3 n1, Vector3 n2,
            Vector2 uv0, Vector2 uv1, Vector2 uv2,
            Bitmap diffuseMap, Bitmap normalMap, Bitmap specularMap) {
            Vector3[] vertices = new Vector3[] { v0, v1, v2 };
            Array.Sort(vertices, (a, b) => a.Y.CompareTo(b.Y));

            Vector3 top = vertices[0];
            Vector3 middle = vertices[1];
            Vector3 bottom = vertices[2];

            int yStart = Math.Max(0, (int)Math.Ceiling(top.Y));
            int yEnd = Math.Min(bitmap.Height - 1, (int)Math.Floor(bottom.Y));

            BitmapData bmpData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadWrite,
                 System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            for (int y = yStart; y <= yEnd; y++)
            {
                float x1, x2;

                if (y <= middle.Y)
                {
                    x1 = InterpolateX(top, middle, y);
                    x2 = InterpolateX(top, bottom, y);
                }
                else
                {
                    x1 = InterpolateX(middle, bottom, y);
                    x2 = InterpolateX(top, bottom, y);
                }

                int xStart = (int)Math.Max(0, Math.Ceiling(Math.Min(x1, x2)));
                int xEnd = (int)Math.Min(bitmap.Width - 1, Math.Floor(Math.Max(x1, x2)));

                if (xStart > xEnd) continue;

                for (int x = xStart; x <= xEnd; x++)
                {
                    float z = InterpolateZ(v0, v1, v2, x, y);

                    if (z <= zBuffer[x, y])
                    {
                        zBuffer[x, y] = z;
                        Barycentric(v0, v1, v2, x, y, out float a, out float c, out float d);

                        Vector3 fragPos = InterpolatePosition(p0, p1, p2, a, c, d);
                        Vector3 normal = InterpolateNormal(n0, n1, n2, a, c, d).Normalize();
                        Vector2 uv = InterpolateUV(uv0, uv1, uv2, a,c, d);

                        Color diffuseColor = SampleTexture(diffuseMap, uv);
                        Vector3 mappedNormal = SampleNormalMap(normalMap, uv).Normalize();
                        float specularStrength = SampleSpecularMap(specularMap, uv);
                        Vector3 lightDir = lightDirection.Normalize();


                        /* float NdotL = Math.Max(0, Vector3.ScalarMultiplication(mappedNormal, lightDir));

                         Vector3 viewDir = (eye - fragPos).Normalize();
                         Vector3 reflectDir = (mappedNormal * 2 * Vector3.ScalarMultiplication(lightDir, mappedNormal) - lightDir).Normalize();
                         float specular = (float)Math.Pow(Math.Max(0, Vector3.ScalarMultiplication(viewDir, reflectDir)), 32) * specularStrength;

                         int r = Math.Clamp((int)(diffuseColor.R * NdotL + 255 * specular), 0, 255);
                         int g = Math.Clamp((int)(diffuseColor.G * NdotL + 255 * specular), 0, 255);
                         int b = Math.Clamp((int)(diffuseColor.B * NdotL + 255 * specular), 0, 255); */

                        float intensity = CalculatePhongIntensity(mappedNormal, fragPos);

                       // можно добавить влияние specularStrength (например, как множитель):
                       intensity *= specularStrength;

                       int r = Math.Clamp((int)(diffuseColor.R * intensity), 0, 255);
                       int g = Math.Clamp((int)(diffuseColor.G * intensity), 0, 255);
                       int b = Math.Clamp((int)(diffuseColor.B * intensity), 0, 255); 


                        unsafe
                        {
                            byte* ptr = (byte*)bmpData.Scan0;
                            int stride = bmpData.Stride;
                            byte* pixel = ptr + y * stride + x * 3;

                            pixel[0] = (byte)b;
                            pixel[1] = (byte)g;
                            pixel[2] = (byte)r;
                        }
                    }
                }
            }

            bitmap.UnlockBits(bmpData);
        }

        public void RasterizeTriangle(Bitmap bitmap, Vector3 v0, Vector3 v1, Vector3 v2,
                              Vector3 n0, Vector3 n1, Vector3 n2,
                              Vector3 p0, Vector3 p1, Vector3 p2)
        {
            Vector3[] vertices = new Vector3[] { v0, v1, v2 };
            Array.Sort(vertices, (a, b) => a.Y.CompareTo(b.Y));

            Vector3 top = vertices[0];
            Vector3 middle = vertices[1];
            Vector3 bottom = vertices[2];

            int yStart = Math.Max(0, (int)Math.Ceiling(top.Y));
            int yEnd = Math.Min(bitmap.Height - 1, (int)Math.Floor(bottom.Y));

            BitmapData bmpData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadWrite,
                System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            for (int y = yStart; y <= yEnd; y++)
            {
                float x1, x2;

                if (y <= middle.Y)
                {
                    x1 = InterpolateX(top, middle, y);
                    x2 = InterpolateX(top, bottom, y);
                }
                else
                {
                    x1 = InterpolateX(middle, bottom, y);
                    x2 = InterpolateX(top, bottom, y);
                }

                int xStart = (int)Math.Max(0, Math.Ceiling(Math.Min(x1, x2)));
                int xEnd = (int)Math.Min(bitmap.Width - 1, Math.Floor(Math.Max(x1, x2)));

                if (xStart > xEnd) continue;


                for (int x = xStart; x <= xEnd; x++)
                {
                    float z = InterpolateZ(v0, v1, v2, x, y);

                    if (z <= zBuffer[x, y])
                    {
                        zBuffer[x, y] = z;
                        Barycentric(v0, v1, v2, x, y, out float a, out float b, out float c);
                        Vector3 normal = InterpolateNormal(n0, n1, n2, a, b, c);
                        Vector3 fragPos = InterpolatePosition(p0, p1, p2, a, b, c);

                        float intensity = CalculatePhongIntensity(normal, fragPos);
                        Vector3 color = ApplyIntensityToColor(intensity);
                        //DrawPixel(bitmap, x, y, color);

                        unsafe
                        {
                            byte* ptr = (byte*)bmpData.Scan0;
                            int stride = bmpData.Stride;

                            byte* pixel = ptr + y * stride + x * 3;
                            pixel[0] = (byte)color.Z;
                            pixel[1] = (byte)color.Y;
                            pixel[2] = (byte)color.X;
                        }
                    }
                }
            }

            bitmap.UnlockBits(bmpData);
        }
        public void RasterizeTriangle(Bitmap bitmap, Vector3 v0, Vector3 v1, Vector3 v2, Vector3 color)
        {
            Vector3[] vertices = new Vector3[] { v0, v1, v2 };
            Array.Sort(vertices, (a, b) => a.Y.CompareTo(b.Y));

            Vector3 top = vertices[0];
            Vector3 middle = vertices[1];
            Vector3 bottom = vertices[2];

            int yStart = Math.Max(0, (int)Math.Ceiling(top.Y));
            int yEnd = Math.Min(bitmap.Height - 1, (int)Math.Floor(bottom.Y));

            for (int y = yStart; y <= yEnd; y++)
            {
                float x1, x2;

                if (y < middle.Y)
                {
                    x1 = InterpolateX(top, middle, y);
                    x2 = InterpolateX(top, bottom, y);
                }
                else
                {
                    x1 = InterpolateX(middle, bottom, y);
                    x2 = InterpolateX(top, bottom, y);
                }

                int xStart = (int)Math.Max(0, Math.Ceiling(Math.Min(x1, x2)));
                int xEnd = (int)Math.Min(bitmap.Width - 1, Math.Floor(Math.Max(x1, x2)));

                if (xStart > xEnd) continue;

                bool flag = false;
                int lastXStart = 0;

                for (int x = xStart; x <= xEnd; x++)
                {
                    float z = InterpolateZ(v0, v1, v2, x, y);

                    if (z < zBuffer[x, y])
                    {
                        zBuffer[x, y] = z;

                        if (!flag)
                        {
                            lastXStart = x;
                            flag = true;
                        }
                    }
                    else if (flag)
                    { 
                        DrawLine(bitmap, new Vector3(lastXStart, y, 0), new Vector3(x - 1, y, 0), color);
                        flag = false;
                    }
                }

                if (flag)
                {
                    DrawLine(bitmap, new Vector3(lastXStart, y, 0), new Vector3(xEnd, y, 0), color);
                }
            }
        }

        /*public void RasterizeTriangle(Bitmap bitmap, Vector3 v0, Vector3 v1, Vector3 v2, Vector3 color)
        {
            Vector3[] vertices = new Vector3[] { v0, v1, v2 };
            Array.Sort(vertices, (a, b) => a.Y.CompareTo(b.Y));

            Vector3 top = vertices[0];
            Vector3 middle = vertices[1];
            Vector3 bottom = vertices[2];

            int yStart = Math.Max(0, (int)Math.Ceiling(top.Y));
            int yEnd = Math.Min(bitmap.Height - 1, (int)Math.Floor(bottom.Y));

            for (int y = yStart; y <= yEnd; y++)
            {
                float x1, x2;

                if (y < middle.Y)
                {
                    x1 = InterpolateX(top, middle, y);
                    x2 = InterpolateX(top, bottom, y);
                }
                else
                {
                    x1 = InterpolateX(middle, bottom, y);
                    x2 = InterpolateX(top, bottom, y);
                }

                int xStart = (int)Math.Max(0, Math.Ceiling(Math.Min(x1, x2)));
                int xEnd = (int)Math.Min(bitmap.Width - 1, Math.Floor(Math.Max(x1, x2)));

                if (xStart > xEnd) continue;

                bool flag = false;
                int lastXStart = 0;

                for (int x = xStart; x <= xEnd; x++)
                {
                    float z = InterpolateZ(v0, v1, v2, x, y);

                    if (z < zBuffer[x, y])
                    {
                        zBuffer[x, y] = z;

                        if (!flag)
                        {
                            lastXStart = x;
                            flag = true;
                        }
                    }
                    else if (flag)
                    {
                        DrawLine(bitmap, new Vector3(lastXStart, y, 0), new Vector3(x - 1, y, 0), color);
                        flag = false;
                    }
                }

                if (flag)
                {
                    DrawLine(bitmap, new Vector3(lastXStart, y, 0), new Vector3(xEnd, y, 0), color);
                }
            }
        }*/

        private float InterpolateX(Vector3 a, Vector3 b, float y)
        {
            if (a.Y == b.Y)
            {
                return a.X;
            }
            return a.X + (b.X - a.X) * (y - a.Y) / (b.Y - a.Y);
        }

        private float InterpolateZ(Vector3 v0, Vector3 v1, Vector3 v2, float x, float y)
        {
            float denominator = (v1.Y - v2.Y) * (v0.X - v2.X) + (v2.X - v1.X) * (v0.Y - v2.Y);
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

        private Vector3 CalculateFaceNormal(Vector3[] v)
        {
            return Vector3.VectorMultiplication(v[1] - v[0], v[2] - v[0]).Normalize();
        }

        private float CalculateLambertIntensity(Vector3 normal)
        {
            float cosTheta = Math.Max(Vector3.ScalarMultiplication(normal, lightDirection), 0.05f);
            cosTheta = Math.Min(cosTheta, 1);
            return cosTheta;
        }
        private float CalculatePhongIntensity(Vector3 normal, Vector3 fragmentPosition)
        {
            float ambientCoefficient = 0.1f; 
            float diffuseCoefficient = 0.7f; 
            float specularCoefficient = 0.2f; 
            int shininess = 32;                

            Vector3 viewDirection = (eye - fragmentPosition).Normalize();
            Vector3 reflectionDirection = (normal * 2 * Vector3.ScalarMultiplication(lightDirection, normal) - lightDirection).Normalize();

            float ambient = ambientCoefficient;

            float diffuse = Math.Max(0, Vector3.ScalarMultiplication(normal, lightDirection)) * diffuseCoefficient;

            float specular = (float)Math.Pow(Math.Max(0, Vector3.ScalarMultiplication(viewDirection, reflectionDirection)), shininess) * specularCoefficient;

            return Math.Clamp(ambient + diffuse + specular, 0f, 1f);
        }

        private Vector3 ApplyIntensityToColor(float intensity)
        {
            byte intensityByte = (byte)(255 * intensity);
            return new Vector3(intensityByte, intensityByte, intensityByte);
        }
        public Bitmap LoadTexture(string filePath)
        {
            return new Bitmap(filePath);
        }
      
        public Color ApplyDiffuseMap(Vector3 vertex, Bitmap diffuseMap, float u, float v)
        {
            int texX = (int)(u * diffuseMap.Width);
            int texY = (int)(v * diffuseMap.Height);
            return diffuseMap.GetPixel(texX, texY);
        }

        public Vector3 ApplyNormalMap(Vector3 vertex, Bitmap normalMap, float u, float v)
        {
            
            int texX = (int)(u * normalMap.Width);
            int texY = (int)(v * normalMap.Height);

            
            Color texColor = normalMap.GetPixel(texX, texY);

            
            float nx = texColor.R / 255.0f * 2.0f - 1.0f;
            float ny = texColor.G / 255.0f * 2.0f - 1.0f;
            float nz = texColor.B / 255.0f * 2.0f - 1.0f;

            return new Vector3(nx, ny, nz);
        }

        public float ApplySpecularMap(Vector3 vertex, Bitmap specularMap, float u, float v)
        {
            
            int texX = (int)(u * specularMap.Width);
            int texY = (int)(v * specularMap.Height);
            
            Color texColor = specularMap.GetPixel(texX, texY);

            return texColor.R / 255.0f;
        }

        public Vector2 InterpolateWithPerspectiveCorrection(Vector2 uv0, Vector2 uv1, float z0, float z1, float t)
        {
            
            float invZ0 = 1.0f / z0;
            float invZ1 = 1.0f / z1;

            float u = (1 - t) * (uv0.X * invZ0) + t * (uv1.X * invZ1);
            float v = (1 - t) * (uv0.Y * invZ0) + t * (uv1.Y * invZ1);

            return new Vector2(u, v);
        }


        private void HandleKeyPress(Keys key, bool isControlPressed)
        {
            switch (key)
            {
                // Повороты
                case Keys.A:
                    rotationY -= rotationSpeed * objectMode;
                    rotateYMatrix = Matricies.GetRotateYMatrix(rotationY);
                    break;

                case Keys.D:
                    rotationY += rotationSpeed * objectMode;
                    rotateYMatrix = Matricies.GetRotateYMatrix(rotationY);
                    break;

                case Keys.W:
                    rotationX -= rotationSpeed * objectMode;
                    rotateXMatrix = Matricies.GetRotateXMatrix(rotationX);
                    break;

                case Keys.S:
                    rotationX += rotationSpeed * objectMode;
                    rotateXMatrix = Matricies.GetRotateXMatrix(rotationX);
                    break;

                case Keys.Q:
                    rotationZ += rotationSpeed * objectMode;
                    rotateZMatrix = Matricies.GetRotateZMatrix(rotationZ);
                    break;

                case Keys.E:
                    rotationZ -= rotationSpeed * objectMode;
                    rotateZMatrix = Matricies.GetRotateZMatrix(rotationZ);
                    break;

                // Перемещения
                case Keys.Up:
                    translationY += translationSpeed * objectMode;
                    break;

                case Keys.Down:
                    translationY -= translationSpeed * objectMode;
                    break;

                case Keys.Left:
                    if (!isControlPressed)
                        translationX -= translationSpeed * objectMode;
                    else
                        translationZ -= translationSpeed * objectMode;
                    break;

                case Keys.Right:
                    if (!isControlPressed)
                        translationX += translationSpeed * objectMode;
                    else
                        translationZ += translationSpeed * objectMode;
                    break;

            }

            // Пересчитываем матрицы трансформации
            translationMatrix = Matricies.GetTranslationMatrix(translationX, translationY, translationZ);

            update();
        }

        private void Scene_Resize(object sender, EventArgs e)
        {
            update();
        }
    }
}
