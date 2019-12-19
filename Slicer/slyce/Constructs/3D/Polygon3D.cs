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
            double middleZ = (z2 + z) / 2;

            //Find minimum and maximum (height) vertex of polygon
            var minV = this.Vertices.Min(v => v.Pos.Z);
            var maxV = this.Vertices.Max(v => v.Pos.Z);
            //Minimum is smaller or equal to the slice line and maximum is greater or equal
            //CASES:
            //A) One vertex is on cutting plane, other on same side (above or below)
            //B) Two vertices are on cutting plane
            //C) One vertex on cutting plane, other on opposite sides (above and below)
            //D) The polygon is above or below the cutting line -> ignore
            //E) One vertex is below the cutting plane, the others are above
            //F) One vertex is above the cutting plane, the others are below
            //G) all vertex are on the cutting plane
            if (SmallerOrEquals(minV, middleZ) && LargerOrEquals(maxV, middleZ))
            {
                //ABCEF
                //Find vertices above, below and on the sliceline
                var above = this.Vertices.Where(v => v.Pos.Z > middleZ && !v.Pos.Z.EpsilonEquals(middleZ, Point.EPSILON)).ToList();
                var below = this.Vertices.Where(v => v.Pos.Z < middleZ && !v.Pos.Z.EpsilonEquals(middleZ, Point.EPSILON)).ToList();
                var equals = this.Vertices.Where(v => v.Pos.Z.EpsilonEquals(middleZ, Point.EPSILON)).ToList();
                if(equals.Count() == 1 && (above.Count() == 2 || below.Count() == 2))
                {
                    //A
                    //Add no lines since the nearby polygon connected to this point should be found too
                    slice_cut = new Line(equals[0].Pos.X, equals[0].Pos.Y, equals[0].Pos.X + Point.EPSILON * 2, equals[0].Pos.Y + Point.EPSILON * 2);
                }
                else if(equals.Count() == 2)
                {
                    //B
                    //Add line between the two vertices
                    slice_cut = new Line(equals[0].Pos.X, equals[0].Pos.Y, equals[1].Pos.X, equals[1].Pos.Y);
                }
                else if(equals.Count() == 1 && above.Count() == 1 && below.Count() == 1)
                {
                    //C
                    //Add line through equals and between above and below
                    var p1 = above[0].Pos;
                    var p2 = below[0].Pos;
                    var x = p1.X + (middleZ - p1.Z) * (p2.X - p1.X) / (p2.Z - p1.Z);
                    var y = p1.Y + (middleZ - p1.Z) * (p2.Y - p1.Y) / (p2.Z - p1.Z);
                    Point point1 = new Point(x, y);
                    Point point2 = new Point(equals[0].Pos.X, equals[0].Pos.Y);
                    if(!point1.Equals(point2))
                    {
                        slice_cut = new Line(point1, point2);
                    }
                    
                }
                else if(below.Count() == 1 && above.Count() == 2)
                {
                    //E
                    //Add line through below and above1 and below and above2
                    var p1 = below[0].Pos;
                    var p2 = above[0].Pos;
                    var x = p1.X + (middleZ - p1.Z) * (p2.X - p1.X) / (p2.Z - p1.Z);
                    var y = p1.Y + (middleZ - p1.Z) * (p2.Y - p1.Y) / (p2.Z - p1.Z);
                    var p3 = above[1].Pos;
                    var x2 = p1.X + (middleZ - p1.Z) * (p3.X - p1.X) / (p3.Z - p1.Z);
                    var y2 = p1.Y + (middleZ - p1.Z) * (p3.Y - p1.Y) / (p3.Z - p1.Z);
                    Point point1 = new Point(x, y);
                    Point point2 = new Point(x2, y2);
                    if (!point1.Equals(point2))
                    {
                        slice_cut = new Line(point1, point2);
                    }

                }
                else if(above.Count() == 1 && below.Count() == 2)
                {
                    //F
                    //Add line through above and below1 and above and below2
                    var p1 = above[0].Pos;
                    var p2 = below[0].Pos;
                    var x = p1.X + (middleZ - p1.Z) * (p2.X - p1.X) / (p2.Z - p1.Z);
                    var y = p1.Y + (middleZ - p1.Z) * (p2.Y - p1.Y) / (p2.Z - p1.Z);
                    var p3 = below[1].Pos;
                    var x2 = p1.X + (middleZ - p1.Z) * (p3.X - p1.X) / (p3.Z - p1.Z);
                    var y2 = p1.Y + (middleZ - p1.Z) * (p3.Y - p1.Y) / (p3.Z - p1.Z);
                    Point point1 = new Point(x, y);
                    Point point2 = new Point(x2, y2);
                    if (!point1.Equals(point2))
                    {
                        slice_cut = new Line(point1, point2);
                    }
                }
                else if(equals.Count() == 3)
                {
                    Polygon2D poly = new Polygon2D();
                    poly.Lines.AddLast(new Line(Vertices[0].Pos.X, Vertices[0].Pos.Y, Vertices[1].Pos.X, Vertices[1].Pos.Y));
                    poly.Lines.AddLast(new Line(Vertices[1].Pos.X, Vertices[1].Pos.Y, Vertices[2].Pos.X, Vertices[2].Pos.Y));
                    poly.Lines.AddLast(new Line(Vertices[2].Pos.X, Vertices[2].Pos.Y, Vertices[0].Pos.X, Vertices[0].Pos.Y));
                    poly.IsSurface = true;
                    slice_cut = poly;

                }
            }
            //D
            //Do nothing

            return slice_cut;
        }
        public IShape2D CutAtZ2(double z, double z2)
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
                if (equals.Count() == 1 && (above.Count() == 2 || below.Count() == 2))
                {
                    if (above.Count() == 0)
                    {
                        above.AddRange(equals);
                    }
                    else if (below.Count() == 0)
                    {
                        below.AddRange(equals);
                    }
                    slice_cut = new Line(equals[0].Pos.X, equals[0].Pos.Y, equals[0].Pos.X + Point.EPSILON * 2, equals[0].Pos.Y + Point.EPSILON * 2);
                }
                else if (equals.Count() == 2)
                {
                    slice_cut = new Line(equals[0].Pos.X, equals[0].Pos.Y, equals[1].Pos.X, equals[1].Pos.Y);
                }
                else if (above.Count == 1 && below.Count == 1 && equals.Count == 1)
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
                        other_point = above.First();
                        list_2_points = below;
                    }
                    else
                    {
                        other_point = below.First();
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
                else if (below.Count > 0 || above.Count > 0 || equals.Count > 0)
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