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

        private Point GetIntersection(Vector p1, Vector p2, double cut_at_z)
        {
            var x = p1.X + (cut_at_z - p1.Z) * (p2.X - p1.X) / (p2.Z - p1.Z);
            var y = p1.Y + (cut_at_z - p1.Z) * (p2.Y - p1.Y) / (p2.Z - p1.Z);
            return new Point(x, y);
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
                var above  = this.Vertices.Where(v => v.Pos.Z > middleZ && !v.Pos.Z.EpsilonEquals(middleZ, Point.EPSILON)).ToList();
                var below  = this.Vertices.Where(v => v.Pos.Z < middleZ && !v.Pos.Z.EpsilonEquals(middleZ, Point.EPSILON)).ToList();
                var equals = this.Vertices.Where(v => v.Pos.Z.EpsilonEquals(middleZ, Point.EPSILON)).ToList();

                if (equals.Count() == 1 && (above.Count() == 2 || below.Count() == 2))
                {
                    //A
                    //Add no lines since the nearby polygon connected to this point should be found too
                    //slice_cut = new Line(equals[0].Pos.X, equals[0].Pos.Y, equals[0].Pos.X + Point.EPSILON * 2, equals[0].Pos.Y + Point.EPSILON * 2);
                }
                else if (equals.Count() == 2)
                {
                    //B
                    //Add line between the two vertices
                    slice_cut = new Line(equals[0].Pos.X, equals[0].Pos.Y, equals[1].Pos.X, equals[1].Pos.Y);
                }
                else if (equals.Count() == 1 && above.Count() == 1 && below.Count() == 1)
                {
                    //C
                    //Add line through equals and between above and below
                    Point point1 = this.GetIntersection(above[0].Pos, below[0].Pos, middleZ);
                    Point point2 = new Point(equals[0].Pos.X, equals[0].Pos.Y);

                    if (!point1.Equals(point2))
                    {
                        slice_cut = new Line(point1, point2);
                    }
                    
                }
                else if(below.Count() == 1 && above.Count() == 2)
                {
                    //E
                    //Add line through below and above1 and below and above2
                    Point point1 = this.GetIntersection(below[0].Pos, above[0].Pos, middleZ);
                    Point point2 = this.GetIntersection(below[0].Pos, above[1].Pos, middleZ);

                    if (!point1.Equals(point2))
                    {
                        slice_cut = new Line(point1, point2);
                    }

                }
                else if(above.Count() == 1 && below.Count() == 2)
                {
                    //F
                    //Add line through above and below1 and above and below2
                    Point point1 = this.GetIntersection(above[0].Pos, below[0].Pos, middleZ);
                    Point point2 = this.GetIntersection(above[0].Pos, below[1].Pos, middleZ);

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