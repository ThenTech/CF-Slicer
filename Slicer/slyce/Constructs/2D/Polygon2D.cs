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

        public bool IsInfill { get; set; } = false;
        public bool IsShell  { get; set; } = false;
        public bool IsOpen   { get; set; } = false;
        public bool IsFloor  { get; set; } = false;
        public bool IsRoof   { get; set; } = false;


        // List of seperate points in the Polygon for ClipperLib
        private Path _IntPoints = null;
        public Path IntPoints
        {
            get
            {
                // Cache list
                if (this.IsComplete() && (_IntPoints == null || _IntPoints.Count != Lines.Count))
                {
                    // Closed poly
                    this._IntPoints = this.Lines.Select(l => l.StartPoint.ToIntPoint()).ToList();
                }
                else if (!this.IsComplete() && (_IntPoints == null || _IntPoints.Count != Lines.Count * 2))
                {
                    // Open poly
                    this._IntPoints = new Path(Lines.Count * 2);

                    foreach (var l in Lines)
                    {
                        this._IntPoints.Add(l.StartPoint.ToIntPoint());
                        this._IntPoints.Add(l.EndPoint.ToIntPoint());
                    }
                }

                return this._IntPoints;
            }
        }

        // Indication of shell offset, higher is more inwards inside object
        public int Shell { get; set; } = 0;

        private Point _Center = null;
        public Point Center
        {
            get
            {
                if (this._Center == null)
                    this._Center = this.GetCenter();
                return this._Center;
            }
        }

        // Used to indicate depth inside slice, i.e. higher is smaller surface area and possibly inside other polies
        public int Hierarchy { get; set; } = 0;

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
            return new Polygon2D(points) {
                IsSurface = this.IsSurface,
                IsContour = this.IsContour,
                Shell     = this.Shell,
                Hierarchy = this.Hierarchy,
                IsInfill  = this.IsInfill,
                IsShell   = this.IsShell,
                IsOpen    = this.IsOpen,
                IsFloor   = this.IsFloor,
                IsRoof    = this.IsRoof
            };
        }

        public Polygon2D(Path path)
        {
            this._IntPoints = path;
            this.UpdateLinesFromPoints();
        }

        public Point FirstPoint()
        {
            return Lines.First.Value.StartPoint;
        }

        public Point LastPoint()
        {
            return Lines.Last.Value.EndPoint;
        }

        public Line First()
        {
            return Lines.First.Value;
        }

        public Line Last()
        {
            return Lines.Last.Value;
        }

        public ConnectionType CanConnect(Line line)
        {
            return this.CanConnect(line, Point.EPSILON);
        }

        public ConnectionType CanConnect(Line line, double precision)
        {
            if (this.Lines.Count > 1)
            {
                if (First().CanConnect(line, precision))
                {
                    if (First().StartPoint.Equals(line.StartPoint, precision))
                    {
                        return ConnectionType.FIRSTREVERSED;
                    }
                    return ConnectionType.FIRST;
                }
                else if (Last().CanConnect(line, precision))
                {
                    if (Last().EndPoint.Equals(line.EndPoint, precision))
                    {
                        return ConnectionType.LASTREVERSED;
                    }
                    return ConnectionType.LAST;
                }
            }
            else
            {
                // Only one Line in poly, so First and Last are the same
                if (First().StartPoint.Equals(line.StartPoint, precision))
                {
                    return ConnectionType.FIRSTREVERSED;
                }
                else if (First().StartPoint.Equals(line.EndPoint, precision))
                {
                    return ConnectionType.FIRST;
                }
                else if (First().EndPoint.Equals(line.EndPoint, precision))
                {
                    return ConnectionType.LASTREVERSED;
                }
                else if (First().EndPoint.Equals(line.StartPoint, precision))
                {
                    return ConnectionType.LAST;
                }
            }

            return ConnectionType.NOT;
        }

        public ConnectionType CanConnect(Polygon2D other)
        {
            return this.CanConnect(other, Point.EPSILON);
        }

        public ConnectionType CanConnect(Polygon2D other, double precision)
        {
            ConnectionType can = ConnectionType.NOT;

            if (other.Lines.Count > 1)
            {
                if ((can = this.CanConnect(other.Last(), precision)) != ConnectionType.NOT)
                {
                    return can;
                }
                else if ((can = this.CanConnect(other.First(), precision)) != ConnectionType.NOT)
                {
                    return can;
                }
            }
            else
            {
                return this.CanConnect(other.First(), precision);
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
            return this.IsComplete(Point.EPSILON);
        }

        public bool IsComplete(double precision)
        {
            return this.Lines.Count > 2
                && (First().StartPoint.Equals(Last().EndPoint, precision)
                 || First().StartPoint.Equals(Last().StartPoint, precision));
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

        public bool AddLine(Line line, ConnectionType connection)
        {
            switch (connection)
            {
                case ConnectionType.NOT:
                    return false;
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
            return true;
        }

        public Polygon2D Transform(Matrix tranformation)
        {
            // Convert to Windows points
            var points = this.IntPoints.Select(po => Point.IntToWinPoint(po)).ToArray();

            // Apply transformation
            tranformation.Transform(points);

            // Converty back to IntPoints
            return new Polygon2D(points.Select(po => Point.WinToIntPoint(po)).ToList());
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
                var end   = this._IntPoints[i];
                lines.AddLast(new Line(start, end) {
                    IsContour = this.IsContour,
                    IsInfill  = this.IsInfill,
                    IsShell   = this.IsShell,
                    IsOpen    = this.IsOpen,
                });
            }

            if (this._IntPoints.Count > 3)
            {
                var first = this._IntPoints.First();
                var last  = this._IntPoints.Last();
                lines.AddLast(new Line(last, first) {
                    IsContour = this.IsContour,
                    IsInfill  = this.IsInfill,
                    IsShell   = this.IsShell,
                    IsOpen    = this.IsOpen,
                });
            }

            this.Lines = lines;
        }

        public void CleanLines()
        {
            if (this.Lines.Count > 2)
            {
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

        public IEnumerable<Polygon2D> CleanToPolygons()
        {
            if (this.Lines.Count > 2)
            {
                this._IntPoints = Clipper.CleanPolygon(this.IntPoints);

                // Optionally also call simplify?
                Paths simplified = Clipper.SimplifyPolygon(this._IntPoints, PolyFillType.pftEvenOdd);
                Clipper c = new Clipper();

                foreach (var s in simplified)
                {
                    c.AddPath(s, PolyType.ptClip, true);
                }

                PolyTree solution = new PolyTree();
                c.Execute(ClipType.ctUnion, solution);

                return PolyNodeToPolies(solution)
                    .Select(p => {
                        p.IsSurface = this.IsSurface;
                        //p.IsContour = this.IsContour;
                        p.Shell     = this.Shell;
                        p.Hierarchy = this.Hierarchy;
                        p.IsInfill  = this.IsInfill;
                        p.IsShell   = this.IsShell;
                        p.IsOpen    = this.IsOpen;
                        p.IsRoof    = this.IsRoof;
                        p.IsFloor   = this.IsFloor;
                        return p;
                    });
            }

            return Enumerable.Empty<Polygon2D>();
        }

        public bool FilterShorts()
        {
            if (this.Lines.Count > 2)
            {
                // Only include consecutive points if their distance is greater than a threshold
                IntPoint p1 = this.IntPoints[0];
                var filtered = new Path(this._IntPoints.Count) { p1 };

                for (var i = 1; i < this._IntPoints.Count; i++)
                {
                    do
                    {
                        // Skip next if too close
                        IntPoint p2 = this._IntPoints[i];

                        if (Line.Distance(p1, p2) < Line.MIN_LENGTH)
                        {
                            i++;
                        }
                        else
                        {
                            p1 = p2;
                            filtered.Add(p2);
                            break;
                        }
                    } while (i < this._IntPoints.Count - 1);
                }

                // Check between last and first points
                if (Line.Distance(filtered[0], filtered[filtered.Count - 1]) < Line.MIN_LENGTH)
                {
                    this._IntPoints.RemoveAt(filtered.Count - 1);
                }

                this._IntPoints = filtered;
                this.UpdateLinesFromPoints();
                return this.Lines.Count > 0;
            }
            else
            {
                // 3 Lines or lower
                if (this.Lines.Count == 0 || (this.Lines.Count == 1 && this.Lines.First.Value.GetLength() < Line.MIN_LENGTH))
                {
                    return false;
                }
                else if (this.Lines.Count == 2)
                {
                    if (this.Lines.First.Value.GetLength() < Line.MIN_LENGTH) this.Lines.RemoveFirst();
                    if (this.Lines.Last.Value.GetLength() < Line.MIN_LENGTH) this.Lines.RemoveLast();
                }
                else
                {
                    var filtered = this.Lines.Where(p => p.GetLength() >= Line.MIN_LENGTH).ToList();

                    if (filtered.Count > 1)
                    {
                        filtered[0].EndPoint = filtered[1].StartPoint;
                    }

                    this.Lines = new LinkedList<Line>(filtered);
                }
            }

            return this.Lines.Count > 0;
        }

        public IEnumerable<Polygon2D> Offset(double delta, double miter_limit = 3)
        {
            var paths = new Paths();

            var c = new ClipperOffset(miter_limit);
            c.AddPath(this.IntPoints, JoinType.jtMiter, this.IsComplete() ? EndType.etClosedPolygon : EndType.etOpenRound);
            c.Execute(ref paths, delta * Point.INT_POINT_FACTOR);

            if (paths.Count > 0)
            {
                foreach (var path in paths)
                {
                    yield return new Polygon2D(path)
                    {
                        IsSurface = this.IsSurface,
                        IsContour = this.IsContour,
                        Shell     = this.Shell,
                        Hierarchy = this.Hierarchy,
                        IsInfill  = this.IsInfill,
                        IsShell   = this.IsShell,
                        IsOpen    = this.IsOpen,
                        IsRoof    = this.IsRoof,
                        IsFloor   = this.IsFloor
                    };
                }
            }
            else
            {
                yield return this;
            }
        }

        public PolyTree GetClipperSolutionWith(IEnumerable<Polygon2D> others, ClipType type)
        {
            // https://github.com/junmer/clipper-lib/blob/master/Documentation.md#clipperlibcliptype
            Clipper c = new Clipper();

            c.AddPath(this.IntPoints, PolyType.ptSubject, this.IsComplete());

            foreach (var other in others)
            {
                // Only closed polys can be used to clip
                if (other.IsComplete())
                    c.AddPath(other.IntPoints, PolyType.ptClip, true);
            }

            PolyTree solution = new PolyTree();

            // intersection, union, difference or XOR
            c.Execute(type, solution);

            return solution;
        }

        private PolyTree GetClipperSolutionWith(Polygon2D other, ClipType type)
        {
            return this.GetClipperSolutionWith(new Polygon2D[1] { other }, type);
        }

        // *******
        internal static void AddPolyNodeToPolies(PolyNode polynode, List<Polygon2D> polies)
        {
            if (polynode.Contour.Count > 0)
            {
                polies.Add(new Polygon2D(polynode.Contour)
                {
                    IsHole  = polynode.IsHole
                });
            }

            foreach (PolyNode pn in polynode.Childs)
            {
                AddPolyNodeToPolies(pn, polies);
            }
        }

        public static IEnumerable<Polygon2D> PolyNodeToPolies(PolyTree polytree)
        {
            var polies = new List<Polygon2D>(polytree.Total);
            AddPolyNodeToPolies(polytree, polies);
            return polies;
        }
        // *******

        public Tuple<bool, IEnumerable<Polygon2D>> Intersects(Polygon2D other)
        {
            if (other.Lines.Count == 0) return Tuple.Create(false, Enumerable.Empty<Polygon2D>());

            var intersection = GetClipperSolutionWith(other, ClipType.ctIntersection);
            return Tuple.Create(intersection.ChildCount > 0 && !this.Contains(other) && !other.Contains(this),
                                PolyNodeToPolies(intersection).Select(p =>
                                {
                                    p.IsSurface = this.IsSurface;
                                    //p.IsContour = this.IsContour;
                                    p.Shell     = this.Shell;
                                    p.Hierarchy = this.Hierarchy;
                                    p.IsInfill  = this.IsInfill;
                                    p.IsShell   = this.IsShell;
                                    return p;
                                }));
        }

        public IEnumerable<Polygon2D> Intersect(IEnumerable<Polygon2D> others)
        {
            var result = GetClipperSolutionWith(others, ClipType.ctIntersection);
            return PolyNodeToPolies(result).Select(p =>
            {
                p.IsSurface = this.IsSurface;
                //p.IsContour = this.IsContour;
                p.Shell     = this.Shell;
                p.Hierarchy = this.Hierarchy;
                p.IsInfill  = this.IsInfill;
                p.IsShell   = this.IsShell;
                return p;
            });
        }

        public IEnumerable<Polygon2D> Subtract(IEnumerable<Polygon2D> others)
        {
            var result = GetClipperSolutionWith(others, ClipType.ctDifference);
            return PolyNodeToPolies(result).Select(p =>
            {
                p.IsSurface = this.IsSurface;
                //p.IsContour = this.IsContour;
                p.Shell     = this.Shell;
                p.Hierarchy = this.Hierarchy;
                p.IsInfill  = this.IsInfill;
                p.IsShell   = this.IsShell;
                return p;
            });
        }

        public IEnumerable<Polygon2D> Subtract(Polygon2D other)
        {
            return Subtract(new Polygon2D[1] { other });
        }

        public IEnumerable<Polygon2D> Union(IEnumerable<Polygon2D> others)
        {
            var result = GetClipperSolutionWith(others, ClipType.ctUnion);
            return PolyNodeToPolies(result).Select(p =>
            {
                p.IsSurface = this.IsSurface;
                //p.IsContour = this.IsContour;
                p.Shell     = this.Shell;
                p.Hierarchy = this.Hierarchy;
                p.IsInfill  = this.IsInfill;
                p.IsShell   = this.IsShell;
                return p;
            });
        }

        public IEnumerable<Polygon2D> Xor(IEnumerable<Polygon2D> others)
        {
            var result = GetClipperSolutionWith(others, ClipType.ctXor);
            return PolyNodeToPolies(result).Select(p =>
            {
                p.IsSurface = this.IsSurface;
                //p.IsContour = this.IsContour;
                p.Shell     = this.Shell;
                p.Hierarchy = this.Hierarchy;
                p.IsInfill  = this.IsInfill;
                p.IsShell   = this.IsShell;
                return p;
            });
        }

        public bool ContainsOrOverlaps(Polygon2D other)
        {
            if (other.Lines.Count == 0) return false;

            var intersection = GetClipperSolutionWith(other, ClipType.ctIntersection);
            return intersection.ChildCount > 0;
        }

        public bool Contains(Polygon2D other)
        {
            if (other.Lines.Count == 0) return false;

            // Check if all points are inside this polygon
            foreach (var p in other.IntPoints)
            {
                // PointInPolygon returns 0 if false, +1 if true, -1 if pt on polygon
                if (Clipper.PointInPolygon(p, this.IntPoints) <= 0)
                    return false;
            }

            return true;
        }

        public double Area()
        {
            return Clipper.Area(this.IntPoints) / (Point.INT_POINT_FACTOR * Point.INT_POINT_FACTOR);
        }

        private Point GetCenter()
        {
            // Adapted from https://stackoverflow.com/a/2792459/6608855

            Point centroid = new Point(0, 0);
            double signedArea = 0.0;

            // For all vertices except last
            foreach (var v in this.Lines)
            {
                var p1 = v.StartPoint;
                var p2 = v.EndPoint;
                var a  = p1.X * p2.Y - p2.X * p1.Y;  // Partial signed area
                signedArea += a;
                centroid.X += (p1.X + p2.X) * a;
                centroid.Y += (p1.Y + p2.Y) * a;
            }

            signedArea /= 2.0;
            centroid.X /= (6.0 * signedArea);
            centroid.Y /= (6.0 * signedArea);

            return centroid;
        }

        public double DistanceTo(Polygon2D other)
        {
            return Line.Distance(this.Center, other.Center);
        }

        public double DistanceFromEndToStart(Polygon2D other)
        {
            return Line.Distance(this.Lines.Last.Value.EndPoint, other.Lines.First.Value.StartPoint);
        }

        public Tuple<bool, double> ResemblesCircle(double tolerance = 1e-5)
        {
            var numPoints = this.IntPoints.Count;

            if (numPoints < 8) return Tuple.Create(false, -1.0);

            Point centroid = this.Center;
            double poly_area = this.Area() * 1.07;  // Adjust for circle

            var p1 = new Point(this.IntPoints[0]);
            var p2 = new Point(this.IntPoints[(int)(numPoints * 1.0 / 3.0)]);
            var p3 = new Point(this.IntPoints[(int)(numPoints * 2.0 / 3.0)]);
            var radius = (Line.Distance(centroid, p1) + Line.Distance(centroid, p2) + Line.Distance(centroid, p2)) / 3.0;

            // May be a circle
            return Tuple.Create(poly_area.EpsilonEquals(Math.PI * radius * radius, 0.8), radius);
        }

        public static int FindClosest(List<Polygon2D> input, Polygon2D from = null) {
            var current = from ?? input[0];
            var start   = from == null ? 1 : 0;

            int closest_index = -1;
            double shortest_dist = Double.MaxValue;

            for (var i = start; i < input.Count; i++)
            {
                //var dist_to = current.DistanceTo(input[i]);
                var dist_to = current.DistanceFromEndToStart(input[i]);
                if (dist_to < shortest_dist)
                {
                    shortest_dist = dist_to;
                    closest_index = i;
                }
            }

            return closest_index;
        }

        public static IEnumerable<Polygon2D> OrderByClosest(List<Polygon2D> input, Polygon2D start = null)
        {
            if (input.Count > 1)
            {
                // Add first
                var current = start ?? input.GetAndRemoveAt(0);
                yield return current;

                while (input.Count > 1)
                {
                    int closest_index = FindClosest(input, current);

                    if (closest_index >= 0)
                    {
                        current = input.GetAndRemoveAt(closest_index);

                        // Add next
                        yield return current;
                    }
                }

                // Add last
                yield return input[0];
            }
        }

        public static void DetermineHierachy(ref List<Polygon2D> input, bool clean_first = true)
        {
            input = Polygon2D.OrderByArea(input, true, clean_first).ToList();

            // Check for containment and flag holes
            for (int i = 0; i < input.Count; i++)
            {
                var poly1 = input[i];
                poly1.Hierarchy = i;

                for (int j = 0; j < input.Count; j++)
                {
                    if (i != j)
                    {
                        var poly2 = input[j];

                        //Check if i contains j
                        if (poly1.Contains(poly2))
                        {
                            if (poly1.IsHole)
                            {
                                poly2.IsHole = false;
                            }
                            else
                            {
                                poly2.IsHole = true;
                            }
                        }
                    }
                }
            }
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
                        List<Polygon2D> pattern = new List<Polygon2D>();
                        var diagonal = Line.Distance(X, Y, X2, Y2);
                        var XStart   = X  - diagonal / 2.0;
                        var XEnd     = X2 + diagonal / 2.0;
                        var Ystart   = Y  - diagonal / 2.0;
                        var YEnd     = Y2 + diagonal / 2.0;

                        // Generate infill rectangles, every length distance, in both X and Y.
                        var x_length = linethickness * pattern_spacing;
                        var y_length = x_length;

                        // Calculate final length of segments to center them in slice
                        var x_amount = (int)Math.Ceiling((size_x - 2.0 * x_length) / x_length);
                        var x_total_length = (x_amount % 2 == 0 ? x_amount - 1 : x_amount) * x_length;

                        var y_amount = (int)Math.Ceiling((size_y - 2.0 * y_length) / y_length);
                        var y_total_length = (y_amount % 2 == 0 ? y_amount - 1 : y_amount) * y_length;

                        // Create pattern going in x-dir  Y + y_offset
                        for (double offset = Ystart + ((size_y - y_total_length) / 2.0); offset <= YEnd - y_length;)
                        {
                            var next_y = offset + y_length;

                            pattern.Add(new Polygon2D(new Line(XStart - x_length, offset, XEnd   + x_length, offset)));
                            pattern.Add(new Polygon2D(new Line(XEnd   + x_length, next_y, XStart - x_length, next_y)));

                            offset = next_y + y_length;
                        }

                        // Transform pattern
                        Matrix matr = Matrix.Identity;
                        matr.RotateAt(-45, X + size_x / 2.0, Y + size_y / 2.0);
                        polies.AddRange(pattern.Select(p => p.Transform(matr)));

                        break;
                    }
                case InfillType.SINGLE_ROTATED:
                    {
                        var pattern = Polygon2D.GenerateInfill(X, Y, X2, Y2, linethickness, pattern_spacing, InfillType.SINGLE);

                        // Transform pattern and add to infill
                        Matrix matr = Matrix.Identity;
                        matr.RotateAt(90, X + size_x / 2.0, Y + size_y / 2.0);
                        polies.AddRange(pattern.Select(p => p.Transform(matr)));

                        break;
                    }
                case InfillType.RECTANGLE:
                    {
                        List<Polygon2D> pattern = new List<Polygon2D>();
                        var diagonal = Line.Distance(X, Y, X2, Y2);
                        var XStart   = X  - diagonal / 2.0;
                        var XEnd     = X2 + diagonal / 2.0;
                        var Ystart   = Y  - diagonal / 2.0;
                        var YEnd     = Y2 + diagonal / 2.0;

                        // Generate infill rectangles, every length distance, in both X and Y.
                        var x_length = linethickness * pattern_spacing;
                        var y_length = x_length;

                        // Calculate final length of segments to center them in slice
                        var x_amount = (int)Math.Ceiling((size_x - 2.0 * x_length) / x_length);
                        var x_total_length = (x_amount % 2 == 0 ? x_amount - 1 : x_amount) * x_length;

                        var y_amount = (int)Math.Ceiling((size_y - 2.0 * y_length) / y_length);
                        var y_total_length = (y_amount % 2 == 0 ? y_amount - 1 : y_amount) * y_length;

                        // Create pattern going in x-dir  Y + y_offset
                        for (double offset = Ystart + ((size_y - y_total_length) / 2.0); offset <= YEnd - y_length;)
                        {
                            var next_y = offset + y_length;
                            var poly = new Polygon2D();

                            poly.AddLine(new Line(XStart - x_length, offset, XEnd   + x_length, offset), ConnectionType.LAST);
                            poly.AddLine(new Line(XEnd   + x_length, offset, XEnd   + x_length, next_y), ConnectionType.LAST);
                            poly.AddLine(new Line(XEnd   + x_length, next_y, XStart - x_length, next_y), ConnectionType.LAST);
                            poly.AddLine(new Line(XStart - x_length, next_y, XStart - x_length, offset), ConnectionType.LAST);

                            pattern.Add(poly);

                            offset = next_y + y_length;
                        }

                        // Transform pattern
                        Matrix matr = Matrix.Identity;
                        matr.RotateAt(-45, X + size_x / 2.0, Y + size_y / 2.0);
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

                            poly.AddLine(new Line(X  - x_length, offset, X2 + x_length, offset), ConnectionType.LAST);
                            poly.AddLine(new Line(X2 + x_length, offset, X2 + x_length, next_y), ConnectionType.LAST);
                            poly.AddLine(new Line(X2 + x_length, next_y, X  - x_length, next_y), ConnectionType.LAST);
                            poly.AddLine(new Line(X  - x_length, next_y, X  - x_length, offset), ConnectionType.LAST);

                            polies.Add(poly);

                            offset = next_y + y_length;
                        }

                        // Create pattern going in y-dir
                        for (offset = X + ((size_x - x_total_length) / 2.0); offset <= X2 - x_length;)
                        {
                            var next_x = offset + x_length;
                            var poly = new Polygon2D();

                            poly.AddLine(new Line(offset, Y  - y_length, offset, Y2 + y_length), ConnectionType.LAST);
                            poly.AddLine(new Line(offset, Y2 + y_length, next_x, Y2 + y_length), ConnectionType.LAST);
                            poly.AddLine(new Line(next_x, Y2 + y_length, next_x, Y  - y_length), ConnectionType.LAST);
                            poly.AddLine(new Line(next_x, Y  - y_length, offset, Y  - y_length), ConnectionType.LAST);

                            polies.Add(poly);

                            offset = next_x + x_length;
                        }

                        break;
                    }
                case InfillType.DIAMOND:
                    {
                        var pattern = Polygon2D.GenerateInfill(X, Y, X2, Y2, linethickness, pattern_spacing, InfillType.SINGLE);

                        // Transform pattern and add to infill
                        Matrix matr = Matrix.Identity;
                        matr.RotateAt(60+45, X + size_x / 2.0, Y + size_y / 2.0);
                        polies.AddRange(pattern.Select(p => p.Transform(matr)));

                        matr.RotateAt(60, X + size_x / 2.0, Y + size_y / 2.0);
                        polies.AddRange(pattern.Select(p => p.Transform(matr)));

                        break;
                    }
                case InfillType.TRIANGLES:
                    {
                        var pattern = Polygon2D.GenerateInfill(X, Y, X2, Y2, linethickness, pattern_spacing, InfillType.SINGLE);

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
                        var pattern = Polygon2D.GenerateInfill(X, Y, X2, Y2, linethickness, pattern_spacing, InfillType.SINGLE);

                        // Transform pattern and add to infill
                        Matrix matr = Matrix.Identity;

                        matr.RotateAt(60, X + size_x / 2.0, Y + size_y / 2.0);
                        polies.AddRange(pattern.Select(p => p.Transform(matr)));

                        matr.RotateAt(60, X + size_x / 2.0, Y + size_y / 2.0);
                        polies.AddRange(pattern.Select(p => p.Transform(matr)));

                        matr.RotateAt(60, X + size_x / 2.0, Y + size_y / 2.0);
                        //matr.Translate(0, (linethickness * pattern_spacing) / 2.0);
                        polies.AddRange(pattern.Select(p => p.Transform(matr)));

                        break;
                    }
                }


            polies.ForEach(p => { p.IsInfill = true; });
            return polies;
        }

        public static IEnumerable<Polygon2D> OrderByArea(IEnumerable<Polygon2D> list, bool bigger_to_small = true, bool clean_first = false)
        {
            var prepare = list.Select(p =>
            {
                if (clean_first) p.CleanLines();
                return Tuple.Create(p, p.Area());
            });

            if (bigger_to_small)
            {
                return prepare.OrderByDescending(t => t.Item2)
                              .Select(t => t.Item1);
            }
            else
            {
                return prepare.OrderBy(t => t.Item2)
                              .Select(t => t.Item1);
            }
        }

        public static IEnumerable<Polygon2D> OrderByHierarchy(IEnumerable<Polygon2D> list, bool outer_to_inner = false)
        {
            return outer_to_inner
                 ? list.OrderBy(p => p.Hierarchy)
                 : list.OrderByDescending(p => p.Hierarchy);
        }
    }
}
