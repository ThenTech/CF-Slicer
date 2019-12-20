using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using ClipperLib;
using Slicer.slyce.Constructs;

namespace Slicer.slyce
{
    internal static class Details
    {
        // https://stackoverflow.com/questions/3524415/get-next-n-elements-from-enumerable
        public static IEnumerable<IList<T>> Chunks<T>(this IEnumerable<T> xs, int size)
        {
            var curr = new List<T>(size);

            foreach (var x in xs)
            {
                curr.Add(x);

                if (curr.Count == size)
                {
                    yield return curr;
                    curr = new List<T>(size);
                }
            }
        }

        public static bool EpsilonEquals(this double x, double y, double epsilon = 1e-15)
        {
            //// Normal
            //return Math.Abs(x - y) < epsilon;

            // About the same with lower epsilon
            double eps = Math.Max(Math.Abs(x), Math.Abs(y)) * epsilon;
            return Math.Abs(x - y) < eps;

            //// Compare to 1
            //if (x == y) return true;
            //var diff = (x + epsilon) / (y + epsilon);
            //var mid = epsilon / 2.0;
            //return diff >= (1.0 - mid) && diff <= (1.0 + mid);
        }

        public static T GetAndRemoveAt<T>(this List<T> input, int index)
        {
            var item = input[index];

            // Put last on old index, and remove
            input[index] = input[input.Count - 1];
            input.RemoveAt(input.Count - 1);

            return item;
        }
    }

    public class Construct
    {
        public Polygon3D[] Polygons { get; private set; }

        public Construct()
        {

        }

        public static Construct Create(Polygon3D[] polygons)
        {
            var obj = new Construct()
            {
                Polygons = polygons
            };
            return obj;
        }

        public static Construct Create(MeshGeometry3D source, Transform3D transform)
        {
            var polies = new List<Polygon3D>();

            // Apply transfomation to all triangles
            var matr = transform.Value;
            var pointClone = source.Positions.ToArray();
            var nrmlClone = source.Normals.ToArray();

            matr.Transform(pointClone);
            matr.Transform(nrmlClone);

            // Sort points by TriangleIndices to be sure
            var points = source.TriangleIndices.Select(
                i => Tuple.Create(new Vector(pointClone[i]),
                                  new Vector(nrmlClone[i]))
            );

            // Take next 3 points and create a Triangle polygon to add
            foreach (var p in points.Chunks(3))
            {
                polies.Add(new Polygon3D(p.Select(t => new Vertex(t.Item1, t.Item2)).ToArray()));
            }

            return Construct.Create(polies.ToArray());
        }

        public MeshGeometry3D ToMesh()
        {
            Point3DCollection points = new Point3DCollection(this.Polygons.Length * 3);
            Vector3DCollection nrmls = new Vector3DCollection(this.Polygons.Length * 3);

            foreach (var p in this.Polygons)
            {
                foreach (var v in p.Vertices)
                {
                    points.Add(v.Pos.ToPoint3D());
                    nrmls.Add(v.Normal.ToVector3D());
                }
            }

            var mesh = new MeshGeometry3D()
            {
                Positions = points,
                Normals = nrmls,
                TriangleIndices = new Int32Collection(Enumerable.Range(0, points.Count).Reverse())
            };

            return mesh;
        }

        public Construct Clone()
        {
            var obj = new Construct()
            {
                Polygons = this.Polygons.Select(p => p.Clone()).ToArray()
            };
            return obj;
        }

        public Slice Slice(double slice_z_height, double perSlice)
        {
            var polies = new List<Polygon2D>();
            var poliesInPlane = new List<Polygon2D>();
            bool HasSurface = false;
            int lookingFor = 7;
            
            foreach (var p in Polygons)
            {
                var cut = p.CutAtZ(slice_z_height, slice_z_height + perSlice);

                if (cut != null && cut.GetType() == typeof(Line))
                {
                    // Got slice line
                    var line = (Line)cut;

                    if (polies.Count > 0)
                    {
                        // Try to add it to a Polygon already
                        bool wasConnected = false;
                        foreach (var po in polies)
                        {
                            var connectType = po.CanConnect(line);
                            if (connectType != ConnectionType.NOT)
                            {
                                wasConnected = true;
                                po.AddLine(line, connectType);
                                break;
                            }
                        }

                        if (!wasConnected)
                        {
                            polies.Add(new Polygon2D(line));
                        }
                    }
                    else
                    {
                        polies.Add(new Polygon2D(line));
                    }
                }
                else if (cut != null && cut.GetType() == typeof(Polygon2D))
                {
                    // Got polygon surface, so this _probably_ is a surface...
                    Polygon2D polygon = (Polygon2D)cut;
                    poliesInPlane.Add(polygon);
                    HasSurface = true;
                    //Fill in polygon
                }
            }

            List<Polygon2D> completePolygons = new List<Polygon2D>();
            List<Polygon2D> needsConnections = new List<Polygon2D>();
            for (int i = 0; i < polies.Count; i++)
            {
                var p = polies[i];

                if (!p.IsComplete() && !p.WasTakenAway)
                {
                    bool connectionFound = false;

                    do
                    {
                        connectionFound = false;

                        if (!p.IsComplete())
                        {
                            // Find another poly that can connect to this one,
                            // until it is closed.
                            for (int j = 0; j < polies.Count; j++)
                            {
                                var p2 = polies[j];

                                if (i != j && !p2.WasTakenAway && p.AddPolygon(p2, p.CanConnect(p2)))
                                {
                                    p2.WasTakenAway = true;
                                    connectionFound = true;
                                    break;
                                }
                            }
                        }
                    } while (connectionFound);

                    if (p.IsComplete())
                    {
                        // IsComplete => take away
                        p.WasTakenAway = true;
                        completePolygons.Add(p);
                    }
                    else
                    {
                        //p.WasTakenAway = true;
                        //Line connect = new Line(p.LastPoint(), p.FirstPoint());
                        //p.AddLine(connect, ConnectionType.LAST);
                        //foreach (var line in p.Lines)
                        //{
                        //    line.IsOpen = true;
                        //}
                        p.IsOpen = true;
                        //completePolygons.Add(p);
                        needsConnections.Add(p);
                    }
                }
                else if (!p.WasTakenAway)
                {
                    // IsComplete => take away
                    p.WasTakenAway = true;
                    completePolygons.Add(p);
                }
            }

            List<Polygon2D> stillNoConnection = new List<Polygon2D>(needsConnections);
            double factor = 10;

            while (stillNoConnection.Count > 0)
            {
                if (stillNoConnection.Count == 1)
                {
                    Line line = new Line(stillNoConnection[0].Last().EndPoint, stillNoConnection[0].First().StartPoint);
                    stillNoConnection[0].AddLine(line, ConnectionType.LAST);
                    stillNoConnection[0].WasTakenAway = true;
                    completePolygons.Add(stillNoConnection[0]);
                    break;
                }

                for (int i = 0; i < stillNoConnection.Count; i++)
                {
                    var p = stillNoConnection[i];

                    if (p.Lines.Count() == 1 && p.Lines.First().GetLength() < Point.EPSILON*factor)
                    {
                        p.WasTakenAway = true;
                        break;
                    }

                    if (!p.IsComplete(Point.EPSILON * factor) && !p.WasTakenAway)
                    {
                        //Try connecting other polygons
                        for (int j = 0; j < stillNoConnection.Count(); j++)
                        {
                            var p2 = stillNoConnection[j];
                            if(i != j)
                            {
                                if (!p2.WasTakenAway && !p2.IsComplete() && p.CanConnect(p2, Point.EPSILON * factor) != ConnectionType.NOT)
                                {
                                    p.AddPolygon(p2, p.CanConnect(p2, Point.EPSILON * factor));
                                    p2.WasTakenAway = true;
                                }
                                if (p.IsComplete())
                                {
                                    p.WasTakenAway = true;
                                    completePolygons.Add(p);
                                    break;
                                }
                            }
                           
                        }
                    }
                    if(p.IsComplete(Point.EPSILON * factor) && !p.WasTakenAway)
                    {
                        Line line = new Line(p.Last().EndPoint, p.First().StartPoint);
                        p.AddLine(line, ConnectionType.LAST);
                        p.WasTakenAway = true;
                        completePolygons.Add(p);
                    }
                }

                factor *= 10;
                stillNoConnection = stillNoConnection.Where(p => !p.IsComplete() && !p.WasTakenAway).ToList();
            }

            //foreach (var p in needsConnections)
            //{
            //    Line line = new Line(p.Last().EndPoint, p.First().StartPoint);
            //    p.AddLine(line, ConnectionType.LAST);
            //}
            //completePolygons.AddRange(needsConnections);
            // Simplify lines and reduce them to a minimum, and sort by area, largest first

            //foreach (var p in completePolygons)
            //{
            //    p.CleanLines();
            //}

            List<Polygon2D> realCompletePolies = new List<Polygon2D>();
            foreach (var p in completePolygons)
            {
                realCompletePolies.AddRange(p.SimplifyToPolygons());
            }

            completePolygons = Polygon2D.OrderByArea(realCompletePolies, true, false).ToList();

            // Check for containment and flag holes
            for (int i = 0; i < completePolygons.Count; i++)
            {
                var poly1 = completePolygons[i];
                poly1.Hierarchy = i;

                for (int j = 0; j < completePolygons.Count; j++)
                {
                    if (i != j)
                    {
                        var poly2 = completePolygons[j];

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

            foreach (var p in completePolygons)
            {
                p.CleanLines();
            }

            //completePolygons = completePolygons.Where(p => p.IsComplete()).ToList();
            return new Slice(completePolygons.Where(p => p.FilterShorts()).ToList(), slice_z_height, HasSurface);
        }
    }
}
