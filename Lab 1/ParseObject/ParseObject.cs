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
                            for (int i = 1; i < tokens.Length; i++)
                            {
                                string[] facesItems = tokens[i].Split('/', StringSplitOptions.RemoveEmptyEntries);
                                for (int j = 0; j < facesItems.Length; j++)
                                {
                                    vertexIndices[i - 1, j] = int.Parse(facesItems[j]);
                                }
                            }
                            Faces.Add(new Face(vertexIndices));
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
        }
    }
}
