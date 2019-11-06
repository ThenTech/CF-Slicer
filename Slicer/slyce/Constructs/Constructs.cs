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
                TriangleIndices = new Int32Collection(Enumerable.Range(0, points.Count))
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
