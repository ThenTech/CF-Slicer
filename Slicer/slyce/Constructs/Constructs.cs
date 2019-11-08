using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public Construct Intersect(Construct other, double slice)
        {
            double Zi = slice;
            var polies = new List<Polygon>();
            var verts = new List<Vertex>();
            foreach (var p in Polygons)
            {
                var minV = p.Vertices.Min(v => v.Pos.Z);
                var maxV = p.Vertices.Max(v => v.Pos.Z);
                if(minV <= Zi && maxV >= Zi)
                {
                    //Find all points above
                    var above = p.Vertices.Where(v => v.Pos.Z > Zi).ToList();
                    var below = p.Vertices.Where(v => v.Pos.Z < Zi).ToList();
                    if (above.Count == 1)
                    {
                        Vertex v1 = null;
                        Vertex v2 = null;
                        Vertex v3 = null;
                        Vertex v4 = null;
                        foreach (var v in below)
                        {
                            var x = v.Pos.X + (Zi - v.Pos.Z) * (above.First().Pos.X - v.Pos.X) / (above.First().Pos.Z - v.Pos.Z);
                            var y = v.Pos.Y + (Zi - v.Pos.Z) * (above.First().Pos.Y - v.Pos.Y) / (above.First().Pos.Z - v.Pos.Z);
                            var z = Zi;
                            var vertex = new Vertex(new Vector(x, y, z), v.Normal);
                            if (v1 == null)
                            {
                                v1 = vertex;
                            }
                            else if (v2 == null)
                            {
                                v2 = vertex;
                            }
                        }
                        v3 = new Vertex(new Vector(v1.Pos.X, v1.Pos.Y, Zi + 0.22), v1.Normal);
                        v4 = new Vertex(new Vector(v2.Pos.X, v2.Pos.Y, Zi + 0.22), v2.Normal);
                        Vertex[] vertices = new Vertex[3] { v1, v2 , v3 };
                        Vertex[] vertices2 = new Vertex[3] { v4, v3, v2 };
                        polies.Add(new Polygon(vertices));
                        polies.Add(new Polygon(vertices2));
                        verts.Add(v1);
                        verts.Add(v2);
                    }
                    else if(below.Count == 1)
                    {
                        Vertex v1 = null;
                        Vertex v2 = null;
                        Vertex v3 = null;
                        Vertex v4 = null;
                        foreach(var v in above)
                        {
                            var x = v.Pos.X + (Zi - v.Pos.Z) * (below.First().Pos.X - v.Pos.X) / (below.First().Pos.Z - v.Pos.Z);
                            var y = v.Pos.Y + (Zi - v.Pos.Z) * (below.First().Pos.Y - v.Pos.Y) / (below.First().Pos.Z - v.Pos.Z);
                            var z = Zi;
                            var vertex = new Vertex(new Vector(x, y, z), v.Normal);
                            if(v1 == null)
                            {
                                v1 = vertex;
                            }
                            else if(v2 == null)
                            {
                                v2 = vertex;
                            }
                        }
                        //v3 = below.First();
                        v3 = new Vertex(new Vector(v1.Pos.X, v1.Pos.Y, Zi + 0.22), v1.Normal);
                        v4 = new Vertex(new Vector(v2.Pos.X, v2.Pos.Y, Zi + 0.22), v2.Normal);
                        Vertex[] vertices = new Vertex[3] { v1, v2, v3 };
                        Vertex[] vertices2 = new Vertex[3] { v4, v3, v2 };
                        polies.Add(new Polygon(vertices));
                        polies.Add(new Polygon(vertices2));
                        verts.Add(v1);
                        verts.Add(v2);
                    }
                }
            }
            //Point3D currentMiddle = new Point3D(0, 0, Zi);
            //foreach (var v in verts)
            //{
            //    currentMiddle.X = (currentMiddle.X + v.Pos.X) / 2;
            //    currentMiddle.Y = (currentMiddle.Y + v.Pos.Y) / 2; 
            //}
            //polies = new List<Polygon>();
            //for (int i = 0; i < verts.Count; i += 2)
            //{
            //    Vertex[] vertices = new Vertex[3] { verts[i], verts[i+1], new Vertex(new Vector(currentMiddle.X, currentMiddle.Y, currentMiddle.Z), new Vector(0,0,1)) };
            //    polies.Add(new Polygon(vertices));
            //}
            return Construct.Create(polies.ToArray());
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
