using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using Slicer.GUI;

namespace Slicer.slyce
{
    public class SliceModel
    {
        private ViewModel data;

        public MeshGeometry3D  Original   { get; private set; }
        public GeometryModel3D SlicePlane { get; private set; }
        public GeometryModel3D Sliced     { get; private set; }
        public Color SliceColour { get; set; }

        public SliceModel(ViewModel data)
        {
            this.data = data;
     
            // https://stackoverflow.com/questions/45452923/get-list-of-all-point3d-in-viewport3d-in-wpf

            var ori = this.data.CurrentModel;

            if (ori.GetType().Equals(typeof(Model3DGroup)))
            {
                ori = (ori as Model3DGroup).Children[0];
            }

            var geom = ori as GeometryModel3D;
            this.Original = geom.Geometry as MeshGeometry3D;
            this.SliceColour = Colors.Red;

            // Raw Points: this.Original.Positions
        }

        public GeometryModel3D UpdateSlice()
        {
            this.BuildSliceBox();
            this.BuildSlice();
            return this.Sliced;
        }

        private void BuildSliceBox()
        {
            // Slice box
            var meshBuilder = new MeshBuilder(false, false);
            var slice_size = 1000;

            // Add very large box
            meshBuilder.AddBox(new Rect3D(-slice_size / 2, -slice_size / 2, this.data.CurrentSliceIdx * this.data.NozzleThickness,
                                          slice_size, slice_size, this.data.NozzleThickness));
            var mesh = meshBuilder.ToMesh(true);
            var greenMaterial = MaterialHelper.CreateMaterial(Colors.Green);
            var insideMaterial = MaterialHelper.CreateMaterial(Colors.Red);

            this.SlicePlane = new GeometryModel3D
            {
                Geometry = mesh,
                Material = greenMaterial,
                BackMaterial = insideMaterial
            };
        }

        private void BuildSlice()
        {
            double thickness = this.data.NozzleThickness;
            double zFrom = this.data.CurrentSliceIdx * thickness;

            var pInf = new Point3D(0, 0, zFrom);
            var nInf = new Vector3D(0, 0, zFrom + thickness);
            Plane3D cpInf = new Plane3D(pInf, nInf);

            var pSup = new Point3D(0, 0, zFrom + thickness);
            var nSup = new Vector3D(0, 0, zFrom + thickness * 2);
            Plane3D cpSup = new Plane3D(pSup, nSup);

            // ???
            var cut = MeshGeometryHelper.Cut(this.Original, pInf, nInf);
            cut = MeshPlaneCut(cut, this.Original, cpInf);

            //var cut = this.Original.Cut(pInf, nInf);


            var cutMaterial = MaterialHelper.CreateMaterial(this.SliceColour);

            this.Sliced = new GeometryModel3D(cut, cutMaterial);
        }

        private static MeshGeometry3D MeshPlaneCut(MeshGeometry3D meshCut, MeshGeometry3D meshOrig, Plane3D plane)
        {
            //Store the positions on the cut plane
            var segments = MeshGeometryHelper.GetContourSegments(meshOrig, plane.Position, plane.Normal).ToList();
            IList<Point3D> vertexPoints = new List<Point3D>();

            if (segments.Count > 0) // TODO is always 0
            {
                //assumes largest contour is the outer contour!
                vertexPoints = MeshGeometryHelper.CombineSegments(segments, 1e-6).ToList().OrderByDescending(x => x.Count).First();

            }

            //meshCut the polygon opening and add to existing cut mesh
            var builder = new MeshBuilder(false, false);
            builder.Append(meshCut.Positions, meshCut.TriangleIndices);
            builder.AddPolygon(vertexPoints);


            MeshGeometry3D mg3D = builder.ToMesh();

            return mg3D;
        }
    }
}
