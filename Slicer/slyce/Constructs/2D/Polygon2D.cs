using ClipperLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slicer.slyce.Constructs._2D
{
    using Path = List<IntPoint>;
    using Paths = List<List<IntPoint>>;

    public class Polygon2D : Shape2D
    {
        public static readonly double INT_POINT_FACTOR = 1000.0;

        // The connected (!) line segments that creates this polygon.
        public LinkedList<Line> Lines { get; set; }

        // Was merged with another poly
        public bool WasTakenAway { get; set; } = false;

        // Indicates that this is an outer surface, and should be completely filled in
        // with lines very close together (=> create a "solid" surface)
        public bool IsSurface { get; set; } = false;

        // Indicates whether this is an outside edge polygon
        public bool IsContour { get; set; } = true;

        // Indicates whether this is an inside edge (hole)
        public bool IsHole { get => !IsContour; set => IsContour = !value; }

        // List of seperate points in the Polygon for ClipperLib
        private Path _IntPoints = null;
        public Path IntPoints
        {
            get
            {
                if (_IntPoints == null || _IntPoints.Count != Lines.Count)
                {
                    // Cache list
                    this._IntPoints = this.Lines.Select(
                        l => new IntPoint((long)(l.StartPoint.X * INT_POINT_FACTOR),
                                          (long)(l.StartPoint.Y * INT_POINT_FACTOR))
                    ).ToList();
                }

                return this._IntPoints;
            }
        }

        public Polygon2D()
        {
            Lines = new LinkedList<Line>();
        }

        public Polygon2D(Line firstLine)
        {
            Lines = new LinkedList<Line>();
            Lines.AddLast(firstLine);
        }

        public Polygon2D(Path path)
        {
            this._IntPoints = path;
            this.UpdateLinesFromPoints();
        }

        public Point FirstPoint()
        {
            return Lines.First().StartPoint;
        }

        public Point LastPoint()
        {
            return Lines.Last().EndPoint;
        }

        public Line First()
        {
            return Lines.First();
        }

        public Line Last()
        {
            return Lines.Last();
        }

        public Connection CanConnect(Line line)
        {
            if (this.Lines.Count > 1)
            {
                if (First().CanConnect(line))
                {
                    if (First().StartPoint.Equals(line.StartPoint))
                    {
                        return Connection.FIRSTREVERSED;
                    }
                    return Connection.FIRST;
                }
                else if (Last().CanConnect(line))
                {
                    if (Last().EndPoint.Equals(line.EndPoint))
                    {
                        return Connection.LASTREVERSED;
                    }
                    return Connection.LAST;
                }
            }
            else
            {
                // Only one Line in poly, so First and Last are the same
                if (First().StartPoint.Equals(line.StartPoint))
                {
                    return Connection.FIRSTREVERSED;
                }
                else if (First().StartPoint.Equals(line.EndPoint))
                {
                    return Connection.FIRST;
                }
                else if (First().EndPoint.Equals(line.EndPoint))
                {
                    return Connection.LASTREVERSED;
                }
                else if (First().EndPoint.Equals(line.StartPoint))
                {
                    return Connection.LAST;
                }
            }

            return Connection.NOT;
        }

        public Connection CanConnect(Polygon2D other)
        {
            Connection can = Connection.NOT;

            if (other.Lines.Count > 1)
            {
                if ((can = this.CanConnect(other.Last())) != Connection.NOT)
                {
                    return can;
                }
                else if ((can = this.CanConnect(other.First())) != Connection.NOT)
                {
                    return can;
                }
            }
            else
            {
                return this.CanConnect(other.First());
            }

            return can;
        }

        public void Swap()
        {
            LinkedList<Line> reversedList = new LinkedList<Line>();
            foreach (var line in Lines)
            {
                line.Swap();
                reversedList.AddFirst(line);
            }
            Lines = reversedList;
        }

        public bool IsComplete()
        {
            return this.Lines.Count > 2
                && (First().StartPoint.Equals(Last().EndPoint) 
                 || First().StartPoint.Equals(Last().StartPoint));
        }

        public bool AddPolygon(Polygon2D poly, Connection connection)
        {
            if (connection != Connection.NOT)
            {
                if (connection == Connection.FIRST || connection == Connection.FIRSTREVERSED)
                {
                    if (connection == Connection.FIRSTREVERSED)
                    {
                        poly.Swap();
                    }
                    foreach (var l in poly.Lines.Reverse())
                    {
                        Lines.AddFirst(l);
                    }
                }
                else if (connection == Connection.LAST || connection == Connection.LASTREVERSED)
                {
                    if (connection == Connection.LASTREVERSED)
                    {
                        poly.Swap();
                    }
                    foreach (var l in poly.Lines)
                    {
                        Lines.AddLast(l);
                    }
                }
                return true;
            }
            return false;
        }

        public void AddLine(Line line, Connection connection)
        {
            switch (connection)
            {
                case Connection.NOT:
                    break;
                case Connection.FIRST:
                    Lines.AddFirst(line);
                    break;
                case Connection.LAST:
                    Lines.AddLast(line);
                    break;
                case Connection.FIRSTREVERSED:
                    line.Swap();
                    Lines.AddFirst(line);
                    break;
                case Connection.LASTREVERSED:
                    line.Swap();
                    Lines.AddLast(line);
                    break;
            }
        }

        public Polygon ToPolygon3D()
        {
            // TODO? Convert to triangles?
            return null;
        }

        //*******************************************************************************
        // Clipper utils
        //*******************************************************************************
        private void UpdateLinesFromPoints()
        {
            var lines = new LinkedList<Line>();

            // Use this._IntPoints to not let it update, and keep the difference in points.
            for (var i = 1; i < this._IntPoints.Count; i++)
            {
                var start = this._IntPoints[i - 1];
                var end = this._IntPoints[i];

                lines.AddLast(new Line((double)start.X / INT_POINT_FACTOR, (double)start.Y / INT_POINT_FACTOR,
                                       (double)end.X / INT_POINT_FACTOR, (double)end.Y / INT_POINT_FACTOR));
            }

            var first = this._IntPoints.First();
            var last = this._IntPoints.Last();
            lines.AddLast(new Line((double)last.X / INT_POINT_FACTOR, (double)last.Y / INT_POINT_FACTOR,
                                   (double)first.X / INT_POINT_FACTOR, (double)first.Y / INT_POINT_FACTOR));

            this.Lines = lines;
        }

        public void CleanLines()
        {
            if (this.Lines.Count > 2)
            {
                // At least a triangle
                this._IntPoints = Clipper.CleanPolygon(this.IntPoints);

                // Optionally also call simplify?
                Paths simplified = Clipper.SimplifyPolygon(this._IntPoints, PolyFillType.pftEvenOdd);
                if (simplified.Count > 0)
                {
                    this._IntPoints = simplified[0];
                }

                this.UpdateLinesFromPoints();
            }
        }

        public void Offset(double delta, double miter_limit = 3)
        {
            Paths li = new Paths
            {
                this.IntPoints
            };

            var paths = Clipper.OffsetPolygons(li, delta * INT_POINT_FACTOR, JoinType.jtMiter, miter_limit);

            if (paths.Count > 0)
            {
                this._IntPoints = paths[0];
                this.UpdateLinesFromPoints();
            }
            else
            {
                // ?
                Console.Error.WriteLine("Poly offset has no result?");
            }
        }

        private PolyTree GetClipperSolutionWith(Polygon2D other, ClipType type)
        {
            // https://github.com/junmer/clipper-lib/blob/master/Documentation.md#clipperlibcliptype

            Clipper c = new Clipper();
            c.AddPolygon(this.IntPoints, PolyType.ptSubject);
            c.AddPolygon(other.IntPoints, PolyType.ptClip);

            PolyTree solution = new PolyTree();

            // intersection, union, difference or XOR
            c.Execute(type, solution);

            return solution;
        }

        public bool Contains(Polygon2D other)
        {
            // ?? TODO
            // If difference is equal to the hole it is inside? 
            var intersection = GetClipperSolutionWith(other, ClipType.ctIntersection);

            if (intersection.ChildCount < 1)
            {
                return false;
            }
            else
            {
                var child = intersection.Childs[0];
                var childIntp = child.Contour;
                var intp = other.IntPoints;
                if (childIntp.Count == intp.Count)
                {
                    for (int i = 0; i < childIntp.Count; i++)
                    {
                        if(childIntp[i].X != intp[i].X || childIntp[i].Y != intp[i].Y)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    return false;
                }
            }
            
            return true;
        }
    }
}
