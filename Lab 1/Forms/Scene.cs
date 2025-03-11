using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
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

        private float[,] rotateXMatrix;
        private float[,] rotateYMatrix;
        private float[,] rotateZMatrix;
        private float[,] scaleMatrix;
        private float[,] translationMatrix;

        private Vector3 eye = new Vector3(0, 0, 5);
        private Vector3 target = new Vector3(0, 0, 0);
        private Vector3 up = new Vector3(0, 1, 0);
        private Vector3 lightDirection = new Vector3(0, -1, 0);

        private const float rotationSpeed = 0.1f;
        private const float translationSpeed = 0.3f;
        private const float speed = 0.005f;
        private System.Windows.Forms.Timer movementTimer;

        private const string city = "Objects\\Castelia City.obj";
        private const string head = "Objects\\scull.obj";
        private const string plant = "Objects\\plant.obj";
        private const string cooler = "Objects\\cooler.obj";
        private const string shark = "Objects\\shark.obj";
        private const string car = "Objects\\car.obj";
        private HashSet<Keys> pressedKeys = new HashSet<Keys>();

        private float[,] zBuffer;

        Bitmap bitmap;

        private int objectMode = 1;

        public Scene()
        {
            InitializeComponent();
            obj = new ObjModel(car);
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
            foreach (var key in pressedKeys)
            {
                HandleKeyPress(key);
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
            float[,] modelMatrix = MathsOperations.MultipleMatrix(
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
            float[,] observerMatrix = Matricies.GetObserverMatrix(eye, target, up);

            // Projection
            //float[,] projectionMatrix = Matricies.GetPerspectiveProjectionMatrix(800f / 600f * 0.2f, 0.2f, 0.1f, 100);
            //(90, 800f / 600f, 1, 1000)
            float[,] projectionMatrix = Matricies.GetPerspectiveProjectionMatrix(float.Pi / 2f, bitmap.Width / (float)bitmap.Height, 1, 1000);

            // Viewport
            float[,] viewportMatrix = Matricies.GetViewingWindowMatrix(bitmap.Width, (float)bitmap.Height, 0, 0);

            var resultMatrix = MathsOperations.MultipleMatrix(
                MathsOperations.MultipleMatrix(
                    MathsOperations.MultipleMatrix(
                        viewportMatrix,
                        projectionMatrix
                        ),
                    observerMatrix
                    ),
                modelMatrix
                );

            // Отрисовка поверхностей
            foreach (var face in obj.Faces)
            {
                int[] coords = { 0, 1, 2 };
                for (int i = 0; i < face.VertexIndices.GetLength(0) - 2; i++)
                {
                    Vector3[] vertices = new Vector3[3];

                    for (int j = 0; j < 3; j++)
                    {
                        int vIndex = face.VertexIndices[coords[j], 0] - 1;
                        Vector3 v = MathsOperations.TransformVertex(obj.Vertices[vIndex], resultMatrix);
                        vertices[j] = new Vector3(v.X / v.W, v.Y / v.W, v.Z / v.W, 1);
                    }

                    Vector3 normal = CalculateFaceNormal(vertices, face);
                    //if (Vector3.ScalarMultiplication(normal, viewDirection) < 0) continue;

                    float intensity = CalculateLambertIntensity(normal);
                    Color color = ApplyIntensityToColor(intensity);

                    RasterizeTriangle(bitmap, vertices[0], vertices[1], vertices[2], color);
                    coords[1]++;
                    coords[2]++;
                }
            }

            // Отрисовка граней
            /*foreach (var face in obj.Faces)
            {
                for (int i = 0; i < face.VertexIndices.GetLength(0); i++)
                {
                    int v1Index = face.VertexIndices[i, 0] - 1;
                    int v2Index = face.VertexIndices[(i + 1) % face.VertexIndices.GetLength(0), 0] - 1;

                    Vector3 v1 = MathsOperations.TransformVertex(obj.Vertices[v1Index], resultMatrix);
                    Vector3 v2 = MathsOperations.TransformVertex(obj.Vertices[v2Index], resultMatrix);

                    v1 = new Vector3(v1.X / v1.W, v1.Y / v1.W, v1.Z / v1.W, 1);
                    v2 = new Vector3(v2.X / v2.W, v2.Y / v2.W, v2.Z / v2.W, 1);

                    DrawLine(bitmap, v1, v2, Color.Blue);
                }
            }*/
            pictureBox1.Image = bitmap;
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

        private void DrawLine(Bitmap bitmap, Vector3 p1, Vector3 p2, Color clr)
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
                        pixel[0] = clr.B;
                        pixel[1] = clr.G;
                        pixel[2] = clr.R;
                    }

                    if (x1 == x2 && y1 == y2) break;

                    int err2 = err * 2;
                    if (err2 > -dy) { err -= dy; x1 += sx; }
                    if (err2 < dx) { err += dx; y1 += sy; }
                }
            }

            bitmap.UnlockBits(bmpData);
        }

        public void RasterizeTriangle(Bitmap bitmap, Vector3 v0, Vector3 v1, Vector3 v2, Color color)
        {
            Vector3[] vertices = new Vector3[] { v0, v1, v2 };
            Array.Sort(vertices, (a, b) => a.Y.CompareTo(b.Y));

            Vector3 top = vertices[0];
            Vector3 middle = vertices[1];
            Vector3 bottom = vertices[2];

            int yStart = (int)Math.Ceiling(top.Y);
            int yEnd = (int)Math.Floor(bottom.Y);

            for (int y = yStart; y <= yEnd; y++)
            {
                if (y < 0 || y >= bitmap.Height) continue;

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

                float x_start = Math.Min(x1, x2);
                float x_end = Math.Max(x1, x2);

                //DrawLine(bitmap, new Vector3(x_start, y, 0), new Vector3(x_end, y, 0), color);
                //////
                bool flag = false;
                int xStart = 0;
                int xEnd = 0;
                for (int x = (int)float.Ceiling(x_start); x <= x_end; x++)
                {
                    float z = InterpolateZ(v0, v1, v2, x, y);
                    if (z < zBuffer[x, y])
                    {
                        zBuffer[x, y] = z;
                        //bitmap.SetPixel(x, y, color);

                        if (!flag)
                        {
                            xStart = x;
                            flag = true;
                        }
                        
                        if (x + 1 > x_end)
                        {
                            xEnd = x;
                            DrawLine(bitmap, new Vector3(xStart, y, 0), new Vector3(xEnd, y, 0), color);
                        }
                    }
                    else
                    {
                        if (flag)
                        {
                            xEnd = x - 1;
                            DrawLine(bitmap, new Vector3(xStart, y, 0), new Vector3(xEnd, y, 0), color);
                            flag = false;
                        }
                    }
                }
                /////
            }
        }

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

        private Vector3 CalculateFaceNormal(Vector3[] v, Face face)
        {
            Vector3 edge1 = v[1] - v[0];
            Vector3 edge2 = v[2] - v[0];

            Vector3 normal = Vector3.VectorMultiplication(edge1, edge2);
            normal.Normalize();
            return normal;
        }

        private float CalculateLambertIntensity(Vector3 normal)
        {
            float cosTheta = Math.Max(Vector3.ScalarMultiplication(normal, lightDirection), 0.4f);
            cosTheta = Math.Min(cosTheta, 1);
            return cosTheta;
        }

        private Color ApplyIntensityToColor(float intensity)
        {
            int r = (int)(255 * intensity);
            int g = (int)(255 * intensity);
            int b = (int)(255 * intensity);
            return Color.FromArgb(r, g, b);
        }

        private void HandleKeyPress(Keys key)
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
                    if (!pressedKeys.Contains(Keys.Control))
                        translationX -= translationSpeed * objectMode;
                    else
                        translationZ -= translationSpeed * objectMode;
                    break;

                case Keys.Right:
                    if (!pressedKeys.Contains(Keys.Control))
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










