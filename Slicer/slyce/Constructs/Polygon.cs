using Slicer.slyce.Constructs._2D;
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

        public Shape2D CutAtZ(double z)
        {
            Line slice_line = null;

            var minV = this.Vertices.Min(v => v.Pos.Z);
            var maxV = this.Vertices.Max(v => v.Pos.Z);

            if (minV <= z && maxV >= z)
            {
                // Find all points above and below
                var above = this.Vertices.Where(v => v.Pos.Z > z).ToList();
                var below = this.Vertices.Where(v => v.Pos.Z <= z).ToList();

                if (above.Count == 1 || below.Count == 1)
                {
                    List<Vertex> list_2_points = null;
                    Vertex other_point = null;
                    
                    if (above.Count == 1)
                    {
                        other_point   = above.First();
                        list_2_points = below;
                    }
                    else
                    {
                        other_point   = below.First();
                        list_2_points = above;
                    }

                    var points = list_2_points.Select(v =>
                    {
                        var x = v.Pos.X + (z - v.Pos.Z) * (other_point.Pos.X - v.Pos.X) / (other_point.Pos.Z - v.Pos.Z);
                        var y = v.Pos.Y + (z - v.Pos.Z) * (other_point.Pos.Y - v.Pos.Y) / (other_point.Pos.Z - v.Pos.Z);
                        return new Point(x, y);
                    }).ToArray();
                    
                    // If points are different, slice line was found
                    if (!points[0].Equals(points[1]))
                    {
                        slice_line = new Line(points[0], points[1]);
                    }
                }
                else if (below.Count == 3 || above.Count == 3)
                {
                    Polygon2D poly = new Polygon2D();
                    poly.Lines.AddLast(new Line(Vertices[0].Pos.X, Vertices[0].Pos.Y, Vertices[1].Pos.X, Vertices[1].Pos.Y));
                    poly.Lines.AddLast(new Line(Vertices[1].Pos.X, Vertices[1].Pos.Y, Vertices[2].Pos.X, Vertices[2].Pos.Y));
                    poly.Lines.AddLast(new Line(Vertices[2].Pos.X, Vertices[2].Pos.Y, Vertices[0].Pos.X, Vertices[0].Pos.Y));
                    return poly;
                    //polies.Add(poly);
                }
            }

            return slice_line;
        }
    }
}