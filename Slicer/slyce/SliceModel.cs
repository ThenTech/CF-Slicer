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
            this.Original = SliceModel.GeometrizeModel(this.data.CurrentModel);
            this.SliceColour = Colors.Red;
        }

        public GeometryModel3D UpdateSlice()
        {
            this.BuildSliceBox();
            this.BuildSlice();
            return this.Sliced;
        }

        public static MeshGeometry3D GeometrizeModel(Model3D source)
        {
            // https://stackoverflow.com/questions/45452923/get-list-of-all-point3d-in-viewport3d-in-wpf
            var ori = source.Clone();

            if (ori.GetType().Equals(typeof(Model3DGroup)))
            {
                ori = (ori as Model3DGroup).Children[0];
            }

            var geom = ori as GeometryModel3D;
            var mesh = geom.Geometry as MeshGeometry3D;

            // To polygon collection
            var poly = Construct.Create(mesh);

            return mesh;
        }

        private void BuildSliceBox()
        {
            // Slice box
            var meshBuilder = new MeshBuilder(false, false);

            // Add box
            var b = this.data.CurrentModel.Bounds;
            meshBuilder.AddBox(new Rect3D(
                b.Location.X, b.Location.Y, 
                b.Location.Z + this.data.CurrentSliceIdx * this.data.NozzleThickness, 
                b.SizeX, b.SizeY, this.data.NozzleThickness
            ));


            var mesh = meshBuilder.ToMesh(false);
            var greenMaterial = MaterialHelper.CreateMaterial(Colors.Green);
            var insideMaterial = MaterialHelper.CreateMaterial(Colors.Red);

            mesh.Normals = mesh.CalculateNormals();

            this.SlicePlane = new GeometryModel3D
            {
                Geometry = mesh,
                Material = greenMaterial,
                BackMaterial = insideMaterial
            };

            this.data.CurrentSlicePlane = this.SlicePlane;
        }

        private void BuildSlice()
        {
            this.Original = SliceModel.GeometrizeModel(this.data.CurrentModel);

            Construct obj = Construct.Create(this.Original);
            Construct box = Construct.Create(this.SlicePlane.Geometry as MeshGeometry3D);

            Construct slice = obj.Intersect(box);

            var cutMaterial = MaterialHelper.CreateMaterial(this.SliceColour);
            this.Sliced = new GeometryModel3D(slice.ToMesh(), cutMaterial);

            this.data.CurrentSlice = this.Sliced;
        }
    }
}
