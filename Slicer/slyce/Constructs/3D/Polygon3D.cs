using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slicer.slyce.Constructs
{
    public class Polygon3D
    {
        public Vertex[] Vertices { get; private set; }

        public Polygon3D(Vertex[] vertices)
        {
            Vertices = vertices;
        }

        public Polygon3D Clone()
        {
            var vertices = Vertices.Select(v => v.Clone()).ToArray();
            return new Polygon3D(vertices);
        }

        public IShape2D CutAtZ(double z, double z2)
        {
            IShape2D slice_cut = null;

            // Put Z slice height in middle of slice block
            z = (z2 - z) / 2 + z;

            var minV = this.Vertices.Min(v => v.Pos.Z);
            var maxV = this.Vertices.Max(v => v.Pos.Z);
            bool poly_within_layer = false;

            if (SmallerOrEquals(minV, z) && LargerOrEquals(maxV, z))
            {
                // Find all points above and below
                var above = this.Vertices.Where(v => v.Pos.Z > z && !v.Pos.Z.EpsilonEquals(z, Point.EPSILON)).ToList();
                var below = this.Vertices.Where(v => v.Pos.Z < z && !v.Pos.Z.EpsilonEquals(z, Point.EPSILON)).ToList();
                var equals = this.Vertices.Where(v => v.Pos.Z.EpsilonEquals(z, Point.EPSILON)).ToList();
                if(equals.Count() >= 1)
                {
                    if(above.Count() == 0)
                    {
                        above.AddRange(equals);
                    }
                    else if(below.Count() == 0)
                    {
                        below.AddRange(equals);
                    }
                    else
                    {
                        int x = 0;

                    }
                }

                if(above.Count == 1 && below.Count == 1 && equals.Count == 1)
                {
                    var x = above[0].Pos.X + (z - above[0].Pos.Z) * (below[0].Pos.X - above[0].Pos.X) / (below[0].Pos.Z - above[0].Pos.Z);
                    var y = above[0].Pos.Y + (z - above[0].Pos.Z) * (below[0].Pos.Y - above[0].Pos.Y) / (below[0].Pos.Z - above[0].Pos.Z);

                    slice_cut = new Line(x, y, equals[0].Pos.X, equals[0].Pos.Y);
                }
                else if (above.Count == 1 || below.Count == 1)
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
                        slice_cut = new Line(points[0], points[1]);
                    }
                }
                else if (below.Count == 3 || above.Count == 3)
                {
                    // Exactly on z
                    poly_within_layer = true;
                }
                else if(below.Count > 0 || above.Count > 0 || equals.Count > 0)
                {
                    Console.WriteLine("EDGE CASE");
                }
            }
            else if (SmallerOrEquals(minV, z2) && LargerOrEquals(maxV, z))
            {
                // All points within z and z2
                poly_within_layer = true;
            }

            if (poly_within_layer)
            {
                Polygon2D poly = new Polygon2D();
                poly.Lines.AddLast(new Line(Vertices[0].Pos.X, Vertices[0].Pos.Y, Vertices[1].Pos.X, Vertices[1].Pos.Y));
                poly.Lines.AddLast(new Line(Vertices[1].Pos.X, Vertices[1].Pos.Y, Vertices[2].Pos.X, Vertices[2].Pos.Y));
                poly.Lines.AddLast(new Line(Vertices[2].Pos.X, Vertices[2].Pos.Y, Vertices[0].Pos.X, Vertices[0].Pos.Y));
                poly.IsSurface = true;
                slice_cut = poly;
            }

            return slice_cut;
        }

        private static bool SmallerOrEquals(double x, double y)
        {
            return x < y || x.EpsilonEquals(y, Point.EPSILON);
        }

        private static bool LargerOrEquals(double x, double y)
        {
            return x > y || x.EpsilonEquals(y, Point.EPSILON);
        }
    }
}