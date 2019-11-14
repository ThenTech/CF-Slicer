using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using ClipperLib;
using Slicer.slyce.Constructs;
using Slicer.slyce.Constructs._2D;

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
    }

    public class Construct
    {
        //public PolyNode[] Polygons { get; private set; }
        public Polygon[] Polygons { get; private set; }

        public Construct()
        {
         
        }

        public static Construct Create(Polygon[] polygons)
        {
            var obj = new Construct()
            {
                Polygons = polygons
            };
            return obj;
        }

        public static Construct Create(MeshGeometry3D source)
        {
            var polies = new List<Polygon>();

            // Sort points by TriangleIndices to be sure
            var points = source.TriangleIndices.Select(
                i => Tuple.Create(new Vector(source.Positions[i]),
                                  new Vector(source.Normals[i]))
            );

            // Take next 3 points and create a Triangle polygon to add
            foreach (var p in points.Chunks(3))
            {
                var triangle = new List<Vertex>();

                foreach (var t in p)
                {
                    triangle.Add(new Vertex(t.Item1, t.Item2));
                }

                polies.Add(new Polygon(triangle.ToArray()));
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

        public Construct Union(Construct other)
        {
            var a = new Node(Clone().Polygons);
            var b = new Node(other.Clone().Polygons);
            a.ClipTo(b);
            b.ClipTo(a);
            b.Invert();
            b.ClipTo(a);
            b.Invert();
            a.Build(b.AllPolygons());
            return Construct.Create(a.AllPolygons());
        }

        public Construct Subtract(Construct other)
        {
            var a = new Node(Clone().Polygons);
            var b = new Node(other.Clone().Polygons);
            a.Invert();
            a.ClipTo(b);
            b.ClipTo(a);
            b.Invert();
            b.ClipTo(a);
            b.Invert();
            a.Build(b.AllPolygons());
            a.Invert();
            return Construct.Create(a.AllPolygons());
        }

        public Construct Slice(double slice_z_height, double perSlice, double minX, double maxX, double minY, double maxY)
        {
            double Zi = slice_z_height;
            var lines = new List<Line>();
            var triangles = new List<Triangle>();
            var polies = new List<Polygon2D>();
            //Line lastLine = null;

            foreach (var p in Polygons)
            {
                var minV = p.Vertices.Min(v => v.Pos.Z);
                var maxV = p.Vertices.Max(v => v.Pos.Z);

                if (minV <= Zi && maxV >= Zi)
                {
                    List<Vertex> list1 = null;
                    List<Vertex> list2 = null;

                    //Find all points above
                    var above = p.Vertices.Where(v => v.Pos.Z > Zi).ToList();
                    var below = p.Vertices.Where(v => v.Pos.Z <= Zi).ToList();

                    if (above.Count == 1 || below.Count == 1)
                    {
                        if (above.Count == 1)
                        {
                            list1 = above;
                            list2 = below;
                        }
                        else
                        {
                            list2 = above;
                            list1 = below;
                        }

                        Point p1 = null;
                        Point p2 = null;

                        foreach (var v in list2)
                        {
                            var x = Math.Round(v.Pos.X + (Zi - v.Pos.Z) * (list1.First().Pos.X - v.Pos.X) / (list1.First().Pos.Z - v.Pos.Z), 3);
                            var y = Math.Round(v.Pos.Y + (Zi - v.Pos.Z) * (list1.First().Pos.Y - v.Pos.Y) / (list1.First().Pos.Z - v.Pos.Z), 3);

                            if (p1 == null)
                            {
                                p1 = new Point(x, y);
                            }
                            else
                            {
                                p2 = new Point(x, y);
                            }
                        }
                        if(!p1.Equals(p2))
                        {
                            var line = new Line(p1, p2);
                            lines.Add(line);
                            if (polies.Count > 0)
                            {
                                var connectionPolygon = polies.FirstOrDefault(po => po.CanConnect(line) != Connection.NOT);
                                if (connectionPolygon != null)
                                {
                                    var connectType = connectionPolygon.CanConnect(line);
                                    connectionPolygon.AddLine(line, connectType);
                                }
                                else
                                {
                                    Polygon2D poly = new Polygon2D(line);
                                    polies.Add(poly);
                                }
                            }
                            else
                            {
                                Polygon2D poly = new Polygon2D(line);
                                polies.Add(poly);
                            }
                        }


                        // Slice has list of Polies
                        // > Look through the existing polies and check if any has the same start- or endpoint
                        //      if found: add line to poly
                        //      if not: Add a new Polygon to the list with the found line
                        // 
                        // Eventually sort created polies with their distance to the middle, so we can print the middle one first
                        
                        

                        //if (!p1.Equals(p2) && (lastLine == null || !lastLine.Equals(line)))
                        //{

                        //    if (lastLine == null || lastLine.Connects(line))
                        //    {
                        //        lines.Add(line);
                        //    }
                        //    else if (lastLine.ReverseConnects(line))
                        //    {
                        //        line.Swap();
                        //        lines.Add(line);
                        //    }
                        //    else if (lastLine.StartPoint.Equals(line.StartPoint))
                        //    {
                        //        lines.Last().Swap();
                        //        lines.Add(line);
                        //    }
                        //    else
                        //    {
                        //        lines.Add(line);
                        //    }

                        //    lastLine = line;
                        //}
                    }
                    else if (below.Count == 3 || above.Count == 3)
                    {
                        triangles.Add(new Triangle(p.Vertices[0].Pos.X, p.Vertices[0].Pos.Y, p.Vertices[1].Pos.X, p.Vertices[1].Pos.Y, p.Vertices[2].Pos.X, p.Vertices[2].Pos.Y));
                    }
                }
            }
            List<bool> takenAway = new List<bool>(new bool[polies.Count]);
            List<Polygon2D> completePolygons = new List<Polygon2D>();
            for(int i = 0; i < polies.Count; i++)
            {
                var p = polies[i];
                if(!p.IsComplete() && !takenAway[i])
                {
                    bool connectionFound = false;
                    do
                    {
                        connectionFound = false;
                        if(!p.IsComplete() && !takenAway[i])
                        {
                            for (int j = 0; j < polies.Count; j++)
                            {
                                if(!takenAway[j])
                                {
                                    var p2 = polies[j];
                                    if (i != j)
                                    {
                                        var connected = p.AddPolygon(p2, p.CanConnect(p2));
                                        if (connected)
                                        {
                                            takenAway[j] = true;
                                            connectionFound = connected;
                                        }
                                    }
                                }
                                

                            }
                        }
                        
                    } while (connectionFound);
                    completePolygons.Add(p);
                }
                else if(!takenAway[i])
                {
                    completePolygons.Add(p);
                }
            }

            //return new Slice(lines, triangles, minX, minY, maxX, maxY);

            List<Polygon> polies = new List<Polygon>();
            triangles.ForEach(t => polies.Add(t.ToPolygon()));

            return Construct.Create(polies.ToArray());
        }

        public Construct Intersect(Construct other)
        {
            var a = new Node(Polygons);
            var b = new Node(other.Polygons);
            a.Invert();
            b.ClipTo(a);
            b.Invert();
            a.ClipTo(b);
            b.ClipTo(a);
            a.Build(b.AllPolygons());
            a.Invert();
            return Construct.Create(a.AllPolygons());
        }

        public Construct Inverse()
        {
            var obj = this.Clone();
            obj.Polygons.ToList().ForEach(p => p.Flip());
            return obj;
        }
    }
}
