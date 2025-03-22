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
        private const string eyeball = "Objects\\eyeball.obj";
        private const string church = "Objects\\church_of_st._tiss.obj";
        private HashSet<Keys> pressedKeys = new HashSet<Keys>();

        private float[,] zBuffer;

        Bitmap bitmap;

        Vector3[] verticesInViewport = new Vector3[3];
        Vector3[] verticecInWorld = new Vector3[3];
        Vector3 v;
        Vector3 u;
        Vector3 normal;
        Vector3 color;
        int vIndex;
        float intensity;

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
            lightDirection = lightDirection * -1;

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
            for (int i = 0; i < count; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    vIndex = obj.faces[i * 3 + j] - 1;
                    v = MathsOperations.TransformVertex(obj.Vertices[vIndex], resultMatrix);

                    verticesInViewport[j].X = v.X / v.W;
                    verticesInViewport[j].Y = v.Y / v.W;
                    verticesInViewport[j].Z = v.Z / v.W;

                    u = MathsOperations.TransformVertex(obj.Vertices[vIndex], modelMatrix);
                    verticecInWorld[j].X = u.X;
                    verticecInWorld[j].Y = u.Y;
                    verticecInWorld[j].Z = u.Z;
                }

                normal = CalculateFaceNormal(verticecInWorld);

                if (Vector3.ScalarMultiplication(normal, (target - eye).Normalize()) > 0) continue;

                intensity = CalculateLambertIntensity(normal);
                /*color = new Vector3((byte)((normal.X + 1) * 0.5f * 255),
                                       (byte)((normal.Y + 1) * 0.5f * 255),
                                       (byte)((normal.Z + 1) * 0.5f * 255));*/
                color = ApplyIntensityToColor(intensity);

                RasterizeTriangle(bitmap, verticesInViewport[0], verticesInViewport[1], verticesInViewport[2], color);
            }

            // Отрисовка ребер
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

                // Определяем x1 и x2 в зависимости от позиции y
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

                    // Проверка глубины
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
                        // Рисуем линию только если она была начата
                        DrawLine(bitmap, new Vector3(lastXStart, y, 0), new Vector3(x - 1, y, 0), color);
                        flag = false;
                    }
                }

                // Если линия была открыта до конца строки
                if (flag)
                {
                    DrawLine(bitmap, new Vector3(lastXStart, y, 0), new Vector3(xEnd, y, 0), color);
                }
            }
        }


        /*
         public void RasterizeTriangle(Bitmap bitmap, Vector3 v0, Vector3 v1, Vector3 v2, Vector3 color)
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
        if (y < 0  y >= bitmap.Height) continue;

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

   
        x_start = Math.Max(0, x_start);
        x_end = Math.Min(bitmap.Width - 1, x_end);

        bool flag = false;
        int xStart = 0;
        int xEnd = 0;

        for (int x = (int)float.Ceiling(x_start); x <= x_end; x++)
        {
      
            if (x < 0  x >= bitmap.Width  y < 0  y >= bitmap.Height) continue;

            float z = InterpolateZ(v0, v1, v2, x, y);
            if (z < zBuffer[x, y])
            {
                zBuffer[x, y] = z;

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
    }
}
         */

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

        private Vector3 ApplyIntensityToColor(float intensity)
        {
            /*int r = (int)(255 * intensity);
            int g = (int)(255 * intensity);
            int b = (int)(255 * intensity);
            return Color.FromArgb(r, g, b);*/

            byte intensityByte = (byte)(255 * intensity);
            return new Vector3(intensityByte, intensityByte, intensityByte);
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
