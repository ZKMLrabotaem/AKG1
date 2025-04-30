using lab1.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab_1.ParseObject
{
    public struct ObjectModel
    {
        public int[] faces;
        public int[] normals;
        public int[] textures;
        public List<Vector3> Vertices;
        public List<Vector2> TextureVertices;
        public List<Vector3> Normals;
    }
}
