using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using Slicer.GUI;
using Slicer.slyce.Constructs._2D;
using ClipperLib;
using Path = System.Collections.Generic.List<ClipperLib.IntPoint>;
using Paths = System.Collections.Generic.List<System.Collections.Generic.List<ClipperLib.IntPoint>>;
using System.Diagnostics;
using System.Threading;

namespace Slicer.slyce
{
    public class SliceModel
    {
        private ViewModel data;

        public MeshGeometry3D  Original   { get; private set; }
        public GeometryModel3D SlicePlane { get; private set; }
        public Slice Slice { get; set; }
        public List<Slice> SliceStore { get; set; }

        public SliceModel(ViewModel data)
        {
            this.data = data;
            this.Original = SliceModel.GeometrizeModel(this.data.CurrentModel);
            this.SliceStore = new List<Slice>();
        }

        public void UpdateSlice()
        {
            this.BuildSliceBox();

            if (this.data.CurrentSliceIdx <= this.SliceStore.Count && this.SliceStore.Count == this.data.MaxSliceIdx+1)
            {
                this.Slice = this.SliceStore[this.data.CurrentSliceIdx];
                this.data.SliceShapes = this.Slice.Shapes;
                this.data.SliceCanvas.Children.Clear();
                this.data.SliceShapes.ForEach(x => this.data.SliceCanvas.Children.Add(x));
                Console.WriteLine("Retreived slice " + this.data.CurrentSliceIdx + " from cache.");
            }
            else
            {
                this.BuildSlice();
                //this.ErodeSlice();

                Console.WriteLine("Created new slice for " + this.data.CurrentSliceIdx + ".");
            }
        }

        public async void BuildAllSlices()
        {
            // Main task is run on another thread, 
            // so that the UI thread does not block and we can update progress bar
            // and display current built slice.

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                this.data.SlicingProgressValue = 0;
                this.data.SlicingInProgress = true;
            });

            this.Original = SliceModel.GeometrizeModel(this.data.CurrentModel);
            var bounds = Original.Bounds;

            this.SliceStore = new List<Slice>(this.data.MaxSliceIdx);

            Construct obj = Construct.Create(this.Original);

            await Task.Run(() =>
            {
                for (int i = 0; i < this.data.MaxSliceIdx + 1; i++)
                {
                    this.Slice = obj.Slice(bounds.Z + i * data.NozzleThickness,
                                           data.NozzleThickness);
                    this.Slice.SetNozzleHeight((i + 1) * data.NozzleThickness);
                    // this.Slice.Erode(data.NozzleThickness / 2.0);

                    var min = Math.Min(bounds.X, bounds.Y);
                    var max = Math.Max(bounds.X + bounds.SizeX, bounds.Y + bounds.SizeY);
                    var size = Math.Min(this.data.SliceCanvas.ActualWidth, this.data.SliceCanvas.ActualHeight);
                    var scale = size / (max - min);

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        this.data.SliceShapes = this.Slice.ToShapes(bounds.X, bounds.Y, scale);

                        this.data.SliceCanvas.Children.Clear();
                        this.data.SliceShapes.ForEach(x => this.data.SliceCanvas.Children.Add(x));

                        this.SliceStore.Add(this.Slice);

                        this.data.SlicingProgressValue = i;
                    });
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    this.data.CurrentSliceIdx = 0;
                    this.data.SlicingInProgress = false;
                });
            });
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
            var bounds = Original.Bounds;

            Construct obj = Construct.Create(this.Original);
            //Construct box = Construct.Create(this.SlicePlane.Geometry as MeshGeometry3D);
            //Construct sli = obj.Intersect(box);

            this.Slice = obj.Slice(bounds.Z + data.CurrentSliceIdx * data.NozzleThickness, 
                                   data.NozzleThickness);

            var min = Math.Min(bounds.X, bounds.Y);
            var max = Math.Max(bounds.X + bounds.SizeX, bounds.Y + bounds.SizeY);
            var size = Math.Min(this.data.SliceCanvas.ActualWidth, this.data.SliceCanvas.ActualHeight);
            var scale = size / (max - min);

            this.data.SliceShapes = this.Slice.ToShapes(bounds.X, bounds.Y, scale);

            this.data.SliceCanvas.Children.Clear();
            this.data.SliceShapes.ForEach(x => this.data.SliceCanvas.Children.Add(x));
        }

        private void ErodeSlice()
        {
            Paths paths = new Paths();
            Path path = new Path();

            foreach (var l in Slice.Lines)
            {
                path.Add(new IntPoint((long)(l.StartPoint.X * 1000), (long)(l.StartPoint.Y * 1000)));
                path.Add(new IntPoint((long)(l.EndPoint.X * 1000), (long)(l.EndPoint.Y * 1000)));
            }

            paths.Add(path);

            Paths result = ClipperLib.Clipper.OffsetPolygons(paths, -1000);
            List<Line> polygonLines = new List<Line>();
            Line lastLine = null;

            foreach (var pol in result)
            {
                List<Point> points = new List<Point>();
                foreach (var p in pol)
                {
                    if(lastLine == null || !lastLine.StartPoint.Equals(lastLine.EndPoint))
                    {
                        lastLine = new Line(p.X/1000.0, p.Y/1000.0, p.X/1000.0, p.Y/1000.0);
                    }
                    else
                    {
                        lastLine.EndPoint = new Point(p.X/1000.0, p.Y/1000.0);
                        polygonLines.Add(lastLine);
                        lastLine = new Line(p.X/1000.0, p.Y/1000.0, p.X/1000.0, p.Y/1000.0);
                    }
                }
                //visualizer.DrawPolygon(points, 1, 0, 18);
            }

            if(polygonLines.Count > 0)
            {
                polygonLines.Add(new Line(polygonLines.Last().EndPoint, polygonLines.First().StartPoint));
                //Slice.Lines.AddRange(polygonLines);
            }
        }
    }
}
