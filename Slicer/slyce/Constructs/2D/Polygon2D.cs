using ClipperLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Slicer.slyce.Constructs
{
    using Path = List<IntPoint>;
    using Paths = List<List<IntPoint>>;

    public class Polygon2D : IShape2D
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

        public int Shell { get; set; } = 1;

        public Polygon2D()
        {
            Lines = new LinkedList<Line>();
        }

        public Polygon2D(Line firstLine)
        {
            Lines = new LinkedList<Line>();
            Lines.AddLast(firstLine);
        }

        public Polygon2D Clone()
        {
            Path points = this.IntPoints.Select(p => new IntPoint(p.X, p.Y)).ToList();
            return new Polygon2D(points) { IsContour = this.IsContour };
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

        public ConnectionType CanConnect(Line line)
        {
            if (this.Lines.Count > 1)
            {
                if (First().CanConnect(line))
                {
                    if (First().StartPoint.Equals(line.StartPoint))
                    {
                        return ConnectionType.FIRSTREVERSED;
                    }
                    return ConnectionType.FIRST;
                }
                else if (Last().CanConnect(line))
                {
                    if (Last().EndPoint.Equals(line.EndPoint))
                    {
                        return ConnectionType.LASTREVERSED;
                    }
                    return ConnectionType.LAST;
                }
            }
            else
            {
                // Only one Line in poly, so First and Last are the same
                if (First().StartPoint.Equals(line.StartPoint))
                {
                    return ConnectionType.FIRSTREVERSED;
                }
                else if (First().StartPoint.Equals(line.EndPoint))
                {
                    return ConnectionType.FIRST;
                }
                else if (First().EndPoint.Equals(line.EndPoint))
                {
                    return ConnectionType.LASTREVERSED;
                }
                else if (First().EndPoint.Equals(line.StartPoint))
                {
                    return ConnectionType.LAST;
                }
            }

            return ConnectionType.NOT;
        }

        public ConnectionType CanConnect(Polygon2D other)
        {
            ConnectionType can = ConnectionType.NOT;

            if (other.Lines.Count > 1)
            {
                if ((can = this.CanConnect(other.Last())) != ConnectionType.NOT)
                {
                    return can;
                }
                else if ((can = this.CanConnect(other.First())) != ConnectionType.NOT)
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

        public bool AddPolygon(Polygon2D poly, ConnectionType connection)
        {
            if (connection != ConnectionType.NOT)
            {
                if (connection == ConnectionType.FIRST || connection == ConnectionType.FIRSTREVERSED)
                {
                    if (connection == ConnectionType.FIRSTREVERSED)
                    {
                        poly.Swap();
                    }
                    foreach (var l in poly.Lines.Reverse())
                    {
                        Lines.AddFirst(l);
                    }
                }
                else if (connection == ConnectionType.LAST || connection == ConnectionType.LASTREVERSED)
                {
                    if (connection == ConnectionType.LASTREVERSED)
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

        public void AddLine(Line line, ConnectionType connection)
        {
            switch (connection)
            {
                case ConnectionType.NOT:
                    break;
                case ConnectionType.FIRST:
                    Lines.AddFirst(line);
                    break;
                case ConnectionType.LAST:
                    Lines.AddLast(line);
                    break;
                case ConnectionType.FIRSTREVERSED:
                    line.Swap();
                    Lines.AddFirst(line);
                    break;
                case ConnectionType.LASTREVERSED:
                    line.Swap();
                    Lines.AddLast(line);
                    break;
            }
        }

        public Polygon2D Transform(Matrix tranformation)
        {
            // Convert to Windows points
            var points = this.IntPoints.Select(po => new System.Windows.Point(po.X / INT_POINT_FACTOR, po.Y / INT_POINT_FACTOR)).ToArray();

            // Apply transformation
            tranformation.Transform(points);

            // Converty back to IntPoints
            return new Polygon2D(points.Select(po => new IntPoint((long)(po.X * INT_POINT_FACTOR),
                                                                  (long)(po.Y * INT_POINT_FACTOR))).ToList());
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

            if (this._IntPoints.Count > 1)
            {
                var first = this._IntPoints.First();
                var last = this._IntPoints.Last();
                lines.AddLast(new Line((double)last.X / INT_POINT_FACTOR, (double)last.Y / INT_POINT_FACTOR,
                                       (double)first.X / INT_POINT_FACTOR, (double)first.Y / INT_POINT_FACTOR));
            }

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

        private PolyTree GetClipperSolutionWith(IEnumerable<Polygon2D> others, ClipType type)
        {
            Clipper c = new Clipper();
            c.AddPolygon(this.IntPoints, PolyType.ptSubject);
            foreach (var other in others)
                c.AddPolygon(other.IntPoints, PolyType.ptClip);

            PolyTree solution = new PolyTree();

            // intersection, union, difference or XOR
            c.Execute(type, solution);

            return solution;
        }

        public bool Intersects(Polygon2D others)
        {
            // Test me
            var result = GetClipperSolutionWith(others, ClipType.ctIntersection);
            return result.Contour.Count != 0;
        }

        public IEnumerable<Polygon2D> Intersect(IEnumerable<Polygon2D> others)
        {
            var result = GetClipperSolutionWith(others, ClipType.ctIntersection);
            return result.ChildCount > 0
                 ? result.Childs.Select(p => new Polygon2D(p.Contour)).ToList()
                 : new List<Polygon2D>();
        }

        public IEnumerable<Polygon2D> Subtract(IEnumerable<Polygon2D> others)
        {
            var result = GetClipperSolutionWith(others, ClipType.ctDifference);
            return result.ChildCount > 0
                 ? result.Childs.Select(p => new Polygon2D(p.Contour)).ToList()
                 : new List<Polygon2D>();
        }

        public IEnumerable<Polygon2D> Union(IEnumerable<Polygon2D> others)
        {
            var result = GetClipperSolutionWith(others, ClipType.ctUnion);
            return result.ChildCount > 0
                 ? result.Childs.Select(p => new Polygon2D(p.Contour)).ToList()
                 : new List<Polygon2D>();
        }

        public IEnumerable<Polygon2D> Xor(IEnumerable<Polygon2D> others)
        {
            var result = GetClipperSolutionWith(others, ClipType.ctXor);
            return result.ChildCount > 0
                 ? result.Childs.Select(p => new Polygon2D(p.Contour)).ToList()
                 : new List<Polygon2D>();
        }

        public bool Contains(Polygon2D other)
        {
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
                        if (childIntp[i].X != intp[i].X || childIntp[i].Y != intp[i].Y)
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

        // (X, Y) -> (X2, Y2) is bounding box corners
        // linethickness == one print line
        // pattern_distribution == Every n printlines, draw pattern
        public static List<Polygon2D> GenerateInfill(double X, double Y, double X2, double Y2, double linethickness, double pattern_spacing = 7.0, InfillType type = InfillType.SQUARE)
        {
            // ?? Single poly with unconnected lines, or List of polys consisting of 1 line?
            List<Polygon2D> polies = new List<Polygon2D>();

            var size_x = Math.Abs(X2 - X);
            var size_y = Math.Abs(Y2 - Y);

            switch (type)
            {
                case InfillType.NONE:
                    break;
                case InfillType.SINGLE:
                    {
                        // Generate infill rectangles, every length distance, in both X and Y.
                        var x_length = linethickness * pattern_spacing;
                        var y_length = x_length;

                        // Calculate final length of segments to center them in slice
                        var x_amount = (int)Math.Ceiling((size_x - 2.0 * x_length) / x_length);
                        var x_total_length = (x_amount % 2 == 0 ? x_amount - 1 : x_amount) * x_length;

                        var y_amount = (int)Math.Ceiling((size_y - 2.0 * y_length) / y_length);
                        var y_total_length = (y_amount % 2 == 0 ? y_amount - 1 : y_amount) * y_length;

                        // Create pattern going in x-dir  Y + y_offset
                        for (double offset = Y + ((size_y - y_total_length) / 2.0); offset <= Y2 - y_length;)
                        {
                            var next_y = offset + y_length;
                            var poly = new Polygon2D();

                            poly.AddLine(new Line(X  - x_length, offset, X2 + x_length, offset), ConnectionType.LAST);
                            poly.AddLine(new Line(X2 + x_length, offset, X2 + x_length, next_y), ConnectionType.LAST);
                            poly.AddLine(new Line(X2 + x_length, next_y, X  - x_length, next_y), ConnectionType.LAST);
                            poly.AddLine(new Line(X  - x_length, next_y, X  - x_length, offset), ConnectionType.LAST);

                            polies.Add(poly);

                            offset = next_y + y_length;
                        }

                        break;
                    }
                case InfillType.SINGLE_ROTATED:
                    {
                        // TODO Surface infill zigzag in other direction

                        // Or just lines
                        var diagonal = new Line(X, Y, X2, Y2).GetLength();
                        var XStart   = X  - diagonal / 2.0;
                        var XEnd     = X2 + diagonal / 2.0;
                        var Ystart   = Y  - diagonal / 2.0;
                        var YEnd     = Y2 + diagonal / 2.0;

                        var pattern = Polygon2D.GenerateInfill(XStart, Ystart, XEnd, YEnd, linethickness, pattern_spacing, InfillType.SINGLE);

                        // Transform pattern and add to infill
                        Matrix matr = Matrix.Identity;
                        matr.RotateAt(90, X + size_x / 2.0, Y + size_y / 2.0);
                        polies.AddRange(pattern.Select(p => p.Transform(matr)));

                        break;
                    }
                case InfillType.SQUARE:
                    {
                        //// Simple square pattern
                        //polies.AddRange(Polygon2D.GenerateInfill(X, Y, X2, Y2, linethickness, pattern_spacing, InfillType.SINGLE));
                        //polies.AddRange(Polygon2D.GenerateInfill(X, Y + (linethickness * pattern_spacing) / 2.0, X2, Y2, linethickness, pattern_spacing, InfillType.SINGLE_ROTATED));
                        //break;

                        // Optimized square pattern
                        var x_length = linethickness * pattern_spacing;
                        var y_length = x_length;

                        // Calculate final length of segments to center them in slice
                        var x_amount = (int)Math.Ceiling((size_x - 2.0 * x_length) / x_length);
                        var x_total_length = (x_amount % 2 == 0 ? x_amount - 1 : x_amount) * x_length;

                        var y_amount = (int)Math.Ceiling((size_y - 2.0 * y_length) / y_length);
                        var y_total_length = (y_amount % 2 == 0 ? y_amount - 1 : y_amount) * y_length;

                        double offset;

                        // Create pattern going in x-dir  Y + y_offset
                        for (offset = Y + ((size_y - y_total_length) / 2.0); offset <= Y2 - y_length;)
                        {
                            var next_y = offset + y_length;
                            var poly = new Polygon2D();

                            poly.AddLine(new Line(X - x_length, offset, X2 + x_length, offset), ConnectionType.LAST);
                            poly.AddLine(new Line(X2 + x_length, offset, X2 + x_length, next_y), ConnectionType.LAST);
                            poly.AddLine(new Line(X2 + x_length, next_y, X - x_length, next_y), ConnectionType.LAST);
                            poly.AddLine(new Line(X - x_length, next_y, X - x_length, offset), ConnectionType.LAST);

                            polies.Add(poly);

                            offset = next_y + y_length;
                        }

                        // Create pattern going in y-dir
                        for (offset = X + ((size_x - x_total_length) / 2.0); offset <= X2 - x_length;)
                        {
                            var next_x = offset + x_length;
                            var poly = new Polygon2D();

                            poly.AddLine(new Line(offset, Y - y_length, offset, Y2 + y_length), ConnectionType.LAST);
                            poly.AddLine(new Line(offset, Y2 + y_length, next_x, Y2 + y_length), ConnectionType.LAST);
                            poly.AddLine(new Line(next_x, Y2 + y_length, next_x, Y - y_length), ConnectionType.LAST);
                            poly.AddLine(new Line(next_x, Y - y_length, offset, Y - y_length), ConnectionType.LAST);

                            polies.Add(poly);

                            offset = next_x + x_length;
                        }

                        break;
                    }
                case InfillType.DIAMOND:
                    {
                        var diagonal = new Line(X, Y, X2, Y2).GetLength();
                        var XStart = X  - diagonal / 2.0;
                        var XEnd   = X2 + diagonal / 2.0;
                        var Ystart = Y  - diagonal / 2.0;
                        var YEnd   = Y2 + diagonal / 2.0;

                        var pattern = Polygon2D.GenerateInfill(XStart, Ystart, XEnd, YEnd, linethickness, pattern_spacing, InfillType.SINGLE);

                        // Transform pattern and add to infill
                        Matrix matr = Matrix.Identity;

                        matr.RotateAt(60, X + size_x / 2.0, Y + size_y / 2.0);
                        polies.AddRange(pattern.Select(p => p.Transform(matr)));

                        matr.RotateAt(60, X + size_x / 2.0, Y + size_y / 2.0);
                        polies.AddRange(pattern.Select(p => p.Transform(matr)));

                        break;
                    }
                case InfillType.TRIANGLES:
                    {
                        var diagonal = new Line(X, Y, X2, Y2).GetLength();
                        var XStart   = X  - diagonal / 2.0;
                        var XEnd     = X2 + diagonal / 2.0;
                        var Ystart   = Y  - diagonal / 2.0;
                        var YEnd     = Y2 + diagonal / 2.0;

                        var pattern = Polygon2D.GenerateInfill(XStart, Ystart, XEnd, YEnd, linethickness, pattern_spacing, InfillType.SINGLE);

                        // Transform pattern and add to infill
                        Matrix matr = Matrix.Identity;

                        matr.RotateAt(60, X + size_x / 2.0, Y + size_y / 2.0);
                        polies.AddRange(pattern.Select(p => p.Transform(matr)));

                        matr.RotateAt(60, X + size_x / 2.0, Y + size_y / 2.0);
                        polies.AddRange(pattern.Select(p => p.Transform(matr)));

                        matr.RotateAt(60, X + size_x / 2.0, Y + size_y / 2.0);
                        matr.Translate(0, (linethickness * pattern_spacing) / 2.0);
                        polies.AddRange(pattern.Select(p => p.Transform(matr)));

                        break;
                    }
                case InfillType.TRI_HEXAGONS:
                    {
                        var diagonal = new Line(X, Y, X2, Y2).GetLength();
                        var XStart   = X  - diagonal / 2.0;
                        var XEnd     = X2 + diagonal / 2.0;
                        var Ystart   = Y  - diagonal / 2.0;
                        var YEnd     = Y2 + diagonal / 2.0;

                        var pattern = Polygon2D.GenerateInfill(XStart, Ystart, XEnd, YEnd, linethickness, pattern_spacing, InfillType.SINGLE);

                        // Transform pattern and add to infill
                        Matrix matr = Matrix.Identity;

                        matr.RotateAt(60, X + size_x / 2.0, Y + size_y / 2.0);
                        polies.AddRange(pattern.Select(p => p.Transform(matr)));

                        matr.RotateAt(60, X + size_x / 2.0, Y + size_y / 2.0);
                        polies.AddRange(pattern.Select(p => p.Transform(matr)));

                        matr.RotateAt(60, X + size_x / 2.0, Y + size_y / 2.0);
                        polies.AddRange(pattern.Select(p => p.Transform(matr)));

                        break;
                    }
                }

            return polies;
        }
    }
}
