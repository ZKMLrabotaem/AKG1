using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab_1.ParseObject
{
    public interface IObject
    {
        public ObjectModel objectModel { get; }
        public List<Vector3> GetCurrentVertices();
    }
}
