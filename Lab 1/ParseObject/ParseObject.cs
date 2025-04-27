using Aspose.ThreeD.Shading;
using lab1.Structures;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lab1.ParseObject
{
    public class ObjModel
    {
        public List<Vector3> Vertices { get; } = new List<Vector3>();
        public List<Face> Faces { get; } = new List<Face>();

        public int[] faces;
        public int[] normals;
        public int[] textures;
        public List<Vector2> TextureVertices { get; } = new List<Vector2>();
        public List<Vector3> Normals { get; } = new List<Vector3>();

        private float minZ = float.MaxValue;
        private float maxTailZ = float.MinValue;

        private List<Vector3> animatedVertices;
        private List<int> tailVerticesIndices = new List<int>();  

        private float amplitude = 0.2f;
        private float frequency = 3f;
        private float time = 0.0f;

        private List<Vector3> previousOffsets;
        private float damping = 0.98f; 
        private float waveSpeed = 4.0f; 

        public ObjModel(string filePath)
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Length == 0) continue;

                    switch (tokens[0])
                    {
                        case "v": // Vertex
                            if (tokens.Length == 4)
                            {
                                float x = float.Parse(tokens[1], CultureInfo.InvariantCulture);
                                float y = float.Parse(tokens[2], CultureInfo.InvariantCulture);
                                float z = float.Parse(tokens[3], CultureInfo.InvariantCulture);
                                Vertices.Add(new Vector3(x, y, z));
                                if (z < minZ) minZ = z;
                            }
                            break;

                        case "f": // Face
                            int[,] vertexIndices = new int[tokens.Length - 1, 3];
                            for (int k = 1; k < tokens.Length; k++)
                            {
                                string[] facesItems = tokens[k].Split('/');
                                for (int j = 0; j < facesItems.Length; j++)
                                {
                                    vertexIndices[k - 1, j] = facesItems[j] != "" ? int.Parse(facesItems[j]) : 0;
                                }
                            }

                            int vertexCount = vertexIndices.GetLength(0);

                            if (vertexCount >= 3)
                            {
                                for (int j = 1; j < vertexCount - 1; j++)
                                {
                                    int[] triangleIndices = new int[3];
                                    int[] normalsIndices = new int[3];
                                    triangleIndices[0] = vertexIndices[0, 0];
                                    triangleIndices[1] = vertexIndices[j, 0];
                                    triangleIndices[2] = vertexIndices[j + 1, 0];

                                    normalsIndices[0] = vertexIndices[0, 2];
                                    normalsIndices[1] = vertexIndices[j, 2];
                                    normalsIndices[2] = vertexIndices[j + 1, 2];

                                    int[] textureIndices = new int[3];
                                    textureIndices[0] = vertexIndices[0, 1];
                                    textureIndices[1] = vertexIndices[j, 1];
                                    textureIndices[2] = vertexIndices[j + 1, 1];

                                    Faces.Add(new Face(triangleIndices, normalsIndices, textureIndices));
                                }
                            }

                            break;

                        case "vt": // Texture coordinates
                            if (tokens.Length == 3)
                            {
                                float x = float.Parse(tokens[1], CultureInfo.InvariantCulture);
                                float y = float.Parse(tokens[2], CultureInfo.InvariantCulture);
                                TextureVertices.Add(new Vector2(x, y));
                            }
                            break;

                        case "vn": // Normals
                            if (tokens.Length == 4)
                            {
                                float x = float.Parse(tokens[1], CultureInfo.InvariantCulture);
                                float y = float.Parse(tokens[2], CultureInfo.InvariantCulture);
                                float z = float.Parse(tokens[3], CultureInfo.InvariantCulture);
                                Normals.Add(new Vector3(x, y, z));
                            }
                            break;
                    }
                }
            }
            animatedVertices = new List<Vector3>(Vertices);
            previousOffsets = new List<Vector3>(new Vector3[Vertices.Count]);

            int facesCount = Faces.Count;
            faces = new int[facesCount * 3];
            normals = new int[facesCount * 3];
            textures = new int[facesCount * 3];
            int i = 0;
            foreach (Face face in Faces)
            {
                for (int j = 0; j < 3; j++)
                {
                    faces[i * 3 + j] = face.VertexIndices[j];
                    normals[i * 3 + j] = face.NormalsIndices[j];
                    textures[i * 3 + j] = face.TextureIndices[j];
                }
                i++;
            }

            float tailThreshold = minZ + (Vertices.Max(v => v.Z) - minZ) * 1f;

            for (int j = 0; j < Vertices.Count; j++)
            {
                if (Vertices[j].Z <= tailThreshold)
                {
                    tailVerticesIndices.Add(j);
                    if (maxTailZ < Vertices[j].Z) maxTailZ = Vertices[j].Z;
                }
            }
        }

        public void Update(float deltaTime)
        {
            time += deltaTime;

            animatedVertices = new List<Vector3>(Vertices);

            for (int i = 0; i < tailVerticesIndices.Count; i++)
            {
                int index = tailVerticesIndices[i];

                float distanceFromBase = Vertices[index].Z - maxTailZ;

                float phase = frequency * time - distanceFromBase * waveSpeed;

                float offsetX = amplitude * distanceFromBase *
                                (float)Math.Sin(phase);

                Vector3 previousOffset = previousOffsets[index];
                Vector3 targetOffset = new Vector3(offsetX, 0, 0);
                Vector3 smoothedOffset = Vector3.Lerp(previousOffset, targetOffset, 0.2f);
                smoothedOffset *= damping;

                previousOffsets[index] = smoothedOffset;

                Vector3 vertex = animatedVertices[index];
                vertex += smoothedOffset;
                animatedVertices[index] = vertex;
            }
        }

        public List<Vector3> GetAnimatedVertices()
        {
            return animatedVertices;
        }
    }
}
