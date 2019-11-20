using System;
using System.Collections.Generic;
using System.Linq;
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

        public static Slice Create(List<Polygon2D> polygons, double z)
        {
            return new Slice(polygons, z);
        }

        public static Construct Create(MeshGeometry3D source, Transform3D transform)
        {
            var polies = new List<Polygon>();

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

        public Slice Slice(double slice_z_height, double perSlice)
        {
            var polies = new List<Polygon2D>();

            foreach (var p in Polygons)
            {
                var cut = p.CutAtZ(slice_z_height);

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
                            if (connectType != Connection.NOT)
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
                else if(cut != null && cut.GetType() == typeof(Polygon2D))
                {
                    // Got polygon surface
                    Polygon2D polygon = (Polygon2D)cut;
                    //Fill in polygon
                }
            }

            List<Polygon2D> completePolygons = new List<Polygon2D>();

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
                }
                else if (!p.WasTakenAway)
                {
                    // IsComplete => take away
                    p.WasTakenAway = true;
                    completePolygons.Add(p);
                }
            }

            // TODO? Sort created polies with their distance to the middle, so we can print the middle one first?
            //  ==> Probably not needed..

            // Simplify lines and reduce them to a minimum
            completePolygons.ForEach(p => p.CleanLines());

            // Check for containment and flag holes
            for (int i = 0; i < completePolygons.Count; i++)
            {
                for (int j = 0; j < completePolygons.Count; j++)
                {
                    if (i != j)
                    {
                        var poly1 = completePolygons[i];
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

            return new Slice(completePolygons, slice_z_height);
        }

        public Construct Intersect(Construct other)
        {
            var a = new Node(Clone().Polygons);
            var b = new Node(other.Clone().Polygons);
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
