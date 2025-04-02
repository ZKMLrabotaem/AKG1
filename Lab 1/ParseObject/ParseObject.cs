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
        public List<Vector2> TextureVertices { get; } = new List<Vector2>();
        public List<Vector3> Normals { get; } = new List<Vector3>();

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

                                    Faces.Add(new Face(triangleIndices, normalsIndices));
                                }
                            }

                            break;

                        /*case "vt": // Texture coordinates
                            if (tokens.Length == 3)
                            {
                                float x = float.Parse(tokens[1], CultureInfo.InvariantCulture);
                                float y = float.Parse(tokens[2], CultureInfo.InvariantCulture);
                                TextureVertices.Add(new Vector2(x, y));
                            }
                            break;*/

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

            int facesCount = Faces.Count;
            faces = new int[facesCount * 3];
            normals = new int[facesCount * 3];
            int i = 0;
            foreach (Face face in Faces)
            {
                for (int j = 0; j < 3; j++)
                {
                    faces[i * 3 + j] = face.VertexIndices[j];
                    normals[i * 3 + j] = face.NormalsIndices[j];
                }
                i++;
            }
        }
    }
}
