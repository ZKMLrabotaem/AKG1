using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lab1.Structures
{
    public struct Face
    {
        public int[] VertexIndices { get; }
        public int[] NormalsIndices { get; }
        public Face(int[] vertexIndices, int[] normalsIndices)
        {
            VertexIndices = vertexIndices;
            NormalsIndices = normalsIndices;
        }
    }
}
