using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using lab1.MatrixOperations;
using lab1.ParseObject;

namespace lab1.Forms
{
    public partial class Scene : Form
    {
        private ObjModel obj;
        private float rotationX = 0;
        private float rotationY = 0;
        private float rotationZ = 0;
        private float scale = 5;
        private float translationX = 0;
        private float translationY = -3.5f;
        private float translationZ = 0;

        private float[,] rotateXMatrix;
        private float[,] rotateYMatrix;
        private float[,] rotateZMatrix;
        private float[,] scaleMatrix;
        private float[,] translationMatrix;

        private Vector3 eye = new Vector3(0, 0, 5);
        private Vector3 target = new Vector3(0, 0, 0);
        private Vector3 up = new Vector3(0, 1, 0);

        private const float rotationSpeed = 0.1f;
        private const float translationSpeed = 0.3f;
        private const float speed = 0.5f;
        private System.Windows.Forms.Timer movementTimer;

        private const string city = "Objects\\Castelia City.obj";
        private const string head = "Objects\\scull.obj";
        private const string plant = "Objects\\plant.obj";
        private const string cooler = "Objects\\cooler.obj";
        private const string shark = "Objects\\shark.obj";
        private HashSet<Keys> pressedKeys = new HashSet<Keys>();

        private int objectMode = 1;

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

            Bitmap bitmap = new Bitmap(this.Width, this.Height);


            bitmap = new Bitmap(ClientSize.Width, ClientSize.Height, PixelFormat.Format24bppRgb);


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
            //(800f/600f*0.2f, 0.2f, 0.1f, 100)
            float[,] projectionMatrix = Matricies.GetPerspectiveProjectionMatrix(90, 800f / 600f, 1, 1000);

            // Viewport
            float[,] viewportMatrix = Matricies.GetViewingWindowMatrix(800, 600, -400, -300);

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

            foreach (var face in obj.Faces)
            {
                for (int i = 0; i < face.VertexIndices.GetLength(0); i++)
                {
                    int v1Index = face.VertexIndices[i, 0] - 1;
                    int v2Index = face.VertexIndices[(i + 1) % face.VertexIndices.GetLength(0), 0] - 1;

                    Vector3 v1 = MathsOperations.TransformVertex(obj.Vertices[v1Index], resultMatrix);
                    Vector3 v2 = MathsOperations.TransformVertex(obj.Vertices[v2Index], resultMatrix);

                    v1 = new Vector3(v1.X / v1.W, v1.Y / v1.W, v1.Z / v1.W, 1);
                    v2 = new Vector3(v2.X / v2.W, v2.Y / v2.W, v2.Z / v2.W, 1);

                    System.Drawing.Point p1 = Project(v1);
                    System.Drawing.Point p2 = Project(v2);

                    DrawLine(bitmap, p1, p2);
                }
            }
            pictureBox1.Image = bitmap;
        }

        private void ClearBitmap(Bitmap bitmap)
        {
            BitmapData bmpData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format24bppRgb);

            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;
                int bytes = bmpData.Stride * bitmap.Height;

                for (int i = 0; i < bytes; i++)
                    ptr[i] = 200; 
            }

            bitmap.UnlockBits(bmpData);
        }

        private System.Drawing.Point Project(Vector3 v)
        {
            float scale = 500 / (500 + v.Z);
            float x = v.X / v.W;
            float y = v.Y / v.W;

            int px = (int)(x * scale + this.ClientSize.Width / 2);
            int py = (int)(y * scale + this.ClientSize.Height / 2);

            return new System.Drawing.Point(px, py);
        }


        /*private void DrawLine(Graphics g, PointF p1, PointF p2)
        {
            using (Pen pen = new Pen(Color.White))
            {
                g.DrawLine(pen, p1, p2);
            }
        }*/

        /*private void DrawLine(Graphics g, PointF p1, PointF p2)
        {
            int x1 = (int)p1.X;
            int y1 = (int)p1.Y;
            int x2 = (int)p2.X;
            int y2 = (int)p2.Y;

            int dx = Math.Abs(x2 - x1);
            int dy = Math.Abs(y2 - y1);
            int sx = (x1 < x2) ? 1 : -1;
            int sy = (y1 < y2) ? 1 : -1; 
            int err = dx - dy; 

            while (true)
            {
                g.FillRectangle(Brushes.White, x1, y1, 1, 1); 

                if (x1  >= 0 && x1 < Width && y1 >= 0 && y1 < Height)
                bmp.SetPixel(x1, y1, Color.White);

                if (x1 == x2 && y1 == y2) break;

                int err2 = err * 2;

                if (err2 > -dy)
                {
                    err -= dy;
                    x1 += sx;
                }
                if (err2 < dx)
                {
                    err += dx;
                    y1 += sy;
                }
            }
        }*/
        private void DrawLine(Bitmap bitmap, System.Drawing.Point p1, System.Drawing.Point p2)
        {
            BitmapData bmpData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadWrite,
                PixelFormat.Format24bppRgb);

            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;
                int stride = bmpData.Stride;
                int x1 = p1.X, y1 = p1.Y;
                int x2 = p2.X, y2 = p2.Y;
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
                        pixel[0] = 255;
                        pixel[1] = 255;
                        pixel[2] = 255;
                    }

                    if (x1 == x2 && y1 == y2) break;

                    int err2 = err * 2;
                    if (err2 > -dy) { err -= dy; x1 += sx; }
                    if (err2 < dx) { err += dx; y1 += sy; }
                }
            }

            bitmap.UnlockBits(bmpData);
        }


        private void DrawAxes(Graphics g)
        {
            int width = this.ClientSize.Width;
            int height = this.ClientSize.Height;

            int centerX = (int)(eye.X + width / 2);
            int centerY = (int)(eye.Y + height / 2);

            int axisLength = Math.Max(width, height) * 2;

            Vector3 zAxis = (target - eye).Normalize();
            Vector3 yAxis = up.Normalize();
            Vector3 xAxis = Vector3.VectorMultiplication(zAxis, yAxis).Normalize();

            DrawLine(g, Pens.Blue, Brushes.Blue, centerX, centerY, xAxis, axisLength);
            g.DrawString("X", new Font("Arial", 12), Brushes.Blue, 0, 0);

            DrawLine(g, Pens.Red, Brushes.Red, centerX, centerY, yAxis, axisLength);
            g.DrawString("Y", new Font("Arial", 12), Brushes.Red, 12, 0);

            DrawLine(g, Pens.Green, Brushes.Green, centerX, centerY, zAxis, axisLength);
            g.DrawString("Z", new Font("Arial", 12), Brushes.Green, 24, 0);
        }

        private void DrawLine(Graphics g, Pen pen, Brush brushes, int centerX, int centerY, Vector3 direction, int length)
        {
            int endX = centerX + (int)(direction.X * length);
            int endY = centerY - (int)(direction.Y * length);
            if (endX != centerX || endY != centerY)
                g.DrawLine(pen, centerX, centerY, endX, endY);
            else
                g.DrawString("X", new Font("Arial", 12), brushes, centerX - 8, centerY - 12);
        }

        private void HandleKeyPress(Keys key)
{
    switch (key)
    {
        // Переключение режима
        case Keys.C:
            objectMode *= -1;
            break;

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

    }
}
