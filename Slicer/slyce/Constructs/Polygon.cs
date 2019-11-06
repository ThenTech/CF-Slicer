using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slicer.slyce.Constructs
{
    public class Polygon
    {
        public Vertex[] Vertices { get; private set; }
        public object Shared     { get; private set; }
        public Plane Plane       { get; private set; }

        public Polygon(Vertex[] vertices, object shared = null)
        {
            Vertices = vertices;
            Shared = shared;
            Plane = Plane.FromPoints(vertices[0].Pos, vertices[1].Pos, vertices[2].Pos);
        }

        public Polygon Clone()
        {
            var vertices = Vertices.Select(v => v.Clone()).ToArray();
            return new Polygon(vertices, Shared);
        }
        
        public void Flip()
        {
            var vertices = Vertices.Reverse();
            vertices.ToList().ForEach(v => v.Flip());

            Vertices = vertices.ToArray();
            Plane.Flip();
        }
    }
}