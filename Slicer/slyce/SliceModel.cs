using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using Slicer.GUI;
using Slicer.slyce.Constructs;
using ClipperLib;
using Path = System.Collections.Generic.List<ClipperLib.IntPoint>;
using Paths = System.Collections.Generic.List<System.Collections.Generic.List<ClipperLib.IntPoint>>;


namespace Slicer.slyce
{
    public class SliceModel
    {
        private ViewModel data;

        private static readonly Brush[] StateBrushes =
        {
            new SolidColorBrush(Color.FromRgb(0x01, 0xD3, 0x28)), 
            Brushes.Yellow, Brushes.Orange, Brushes.OrangeRed, Brushes.Crimson
        };

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
                //Console.WriteLine("Retreived slice " + this.data.CurrentSliceIdx + " from cache.");
            }
            else
            {
                this.BuildSlice();
                //Console.WriteLine("Created new slice for " + this.data.CurrentSliceIdx + ".");
            }
        }

        public void RedrawAllSlices()
        {
            var bounds = this.data.CurrentModel.Bounds;
            var min = Math.Min(bounds.X, bounds.Y);
            var max = Math.Max(bounds.X + bounds.SizeX, bounds.Y + bounds.SizeY);
            var size = Math.Min(this.data.SliceCanvas.ActualWidth, this.data.SliceCanvas.ActualHeight);
            var scale = size / (max - min);

            if (this.SliceStore.Count == this.data.MaxSliceIdx + 1)
            {
                foreach (var slice in this.SliceStore)
                {
                    slice.Shapes = null;
                    slice.ToShapes(bounds.X, bounds.Y, scale,
                                   this.data.PreviewArrowThickness, this.data.PreviewStrokeThickness);

                }
                this.UpdateSlice();
            }
            else if (this.Slice != null)
            {
                this.Slice.Shapes = null;
                this.data.SliceShapes = this.Slice.ToShapes(bounds.X, bounds.Y, scale,
                                                            this.data.PreviewArrowThickness, this.data.PreviewStrokeThickness);
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
                this.data.ProgressBarColor = SliceModel.StateBrushes[0];
            });

            this.Original = SliceModel.GeometrizeModel(this.data.CurrentModel);
            var bounds = this.data.CurrentModel.Bounds;
            var transform = this.data.CurrentModel.Transform;

            var min   = Math.Min(bounds.X, bounds.Y);
            var max   = Math.Max(bounds.X + bounds.SizeX, bounds.Y + bounds.SizeY);
            var size  = Math.Min(this.data.SliceCanvas.ActualWidth, this.data.SliceCanvas.ActualHeight);
            var scale = size / (max - min);

            var dense_spacing = this.data.NozzleDiameter * 2.375;  // Default dense_spacing == 0.95

            this.SliceStore = Enumerable.Repeat<Slice>(null, this.data.MaxSliceIdx + 1).ToList();

            Construct obj = Construct.Create(this.Original, transform);

            // Genarate infills
            var infill_struct = Polygon2D.GenerateInfill(
                bounds.X, bounds.Y,
                bounds.X + bounds.SizeX, bounds.Y + bounds.SizeY,
                this.data.NozzleDiameter, this.data.InfillSpacing, this.data.UseInfill
            );

            var surface_struct = Polygon2D.GenerateInfill(
                bounds.X, bounds.Y,
                bounds.X + bounds.SizeX, bounds.Y + bounds.SizeY,
                this.data.NozzleDiameter, dense_spacing, InfillType.SINGLE
            );

            var surface_struct_alt = Polygon2D.GenerateInfill(
                bounds.X, bounds.Y,
                bounds.X + bounds.SizeX, bounds.Y + bounds.SizeY,
                this.data.NozzleDiameter, dense_spacing, InfillType.SINGLE_ROTATED
            );

            await Task.Run(() =>
            {
                //// For debug, set `opt` to 1, else -1 for unlimited
                var opt = new ParallelOptions() { MaxDegreeOfParallelism = -1 };

                // Execute slicing
                // Step 1: Find contours by slicing with Z plane and Erode
                Parallel.For(0, this.data.MaxSliceIdx + 1, opt, (i) => {
                    // Construct slice
                    var slice = obj.Slice(bounds.Z + i * data.NozzleThickness,
                                          data.NozzleThickness);
                    slice.SetNozzleHeight((i + 1) * data.NozzleThickness);
                    slice.Erode(data.NozzleDiameter / 2.0);
                    this.SliceStore[i] = slice;

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        this.data.SlicingProgressValue++;
                    });
                });

                // Adjust Nozzle height to ignore empty bottom layers
                var first_index = this.SliceStore.FindIndex(s => s.Polygons.Count > 0);
                if (first_index > 0)
                {
                    for (int i = 0; i < this.data.MaxSliceIdx + 1 - first_index; i++)
                    {
                        this.SliceStore[i + first_index].SetNozzleHeight((i + 1) * data.NozzleThickness);
                    }
                }
                
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    this.data.SlicingProgressValue = 0;
                    this.data.ProgressBarColor = SliceModel.StateBrushes[1];
                });


                // Step 2: Determine surfaces
                //         Compare with layer above and below to find and add floor/roofs.
                Parallel.For(0, this.data.MaxSliceIdx + 1, opt, (i) => {
                    var slice = this.SliceStore[i];

                    // Determine surfaces
                    slice.DetermineSurfaces(this.SliceStore.ElementAtOrDefault(i - 1),
                                            this.SliceStore.ElementAtOrDefault(i + 1));

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        this.data.SlicingProgressValue++;
                    });
                });


                Parallel.For(0, this.data.MaxSliceIdx + 1, opt, (i) =>
                {
                    this.SliceStore[i].AddFoundSurfaces();
                });

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    this.data.SlicingProgressValue = 0;
                    this.data.ProgressBarColor = SliceModel.StateBrushes[2];
                });


                // Step 3: Propagate roof/floors
                var floors = new List<Polygon2D>();
                var roofs  = new List<Polygon2D>();

                for (int i = 0; i < this.data.MaxSliceIdx + 1; i++)
                {
                    var from_below = this.SliceStore.ElementAtOrDefault(i);
                    var from_above = this.SliceStore.ElementAtOrDefault(this.data.MaxSliceIdx - i);

                    if (from_below != null && from_below.Polygons.Count > 0)
                    {
                        // Get floors that need to be propagated
                        var propagate = from_below.Polygons
                                            .Where(p => p.IsSurface && p.IsFloor)
                                            .Select(p => { p.Shell = this.data.NumberOfShells - 1; return p; })
                                            .ToList();  // To list, else enumerator will select floors added in next statement

                        // Add floor polies from current that need to go up
                        from_below.Polygons.AddRange(
                            floors.Where(p => p.Shell > 0)
                                  .Select(p => { p.Shell--; return p; }));

                        floors.AddRange(propagate);
                    }

                    if (from_above != null && from_above.Polygons.Count > 0)
                    {
                        // Get roofs that need to be propagated
                        var propagate = from_above.Polygons
                                            .Where(p => p.IsSurface && p.IsRoof)
                                            .Select(p => { p.Shell = this.data.NumberOfShells - 1; return p; })
                                            .ToList();  // To list, else enumerator will select roofs added in next statement

                        // Add roof polies from current that need to go down
                        from_above.Polygons.AddRange(
                            roofs.Where(p => p.Shell > 0)
                                 .Select(p => { p.Shell--; return p; }));

                        roofs.AddRange(propagate);
                    }

                    floors = floors.Where(p => p.Shell > 0).ToList();
                    roofs = roofs.Where(p => p.Shell > 0).ToList();

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        this.data.SlicingProgressValue++;
                    });
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    this.data.SlicingProgressValue = 0;
                    this.data.ProgressBarColor = SliceModel.StateBrushes[3];
                });

                // Step 4: Generate Support
                Parallel.For(0, this.data.MaxSliceIdx + 1, opt, (i) =>
                {
                    var j = this.data.MaxSliceIdx - i;

                    var current = this.SliceStore.ElementAtOrDefault(j);
                    var above   = this.SliceStore.ElementAtOrDefault(j + 1);

                    current.GenerateSupport(above, this.data.NozzleThickness);

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        this.data.SlicingProgressValue++;
                    });
                });

                Parallel.For(0, this.data.MaxSliceIdx + 1, opt, (i) =>
                {
                    this.SliceStore[i].AddFoundSurfaces();
                });

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    this.data.SlicingProgressValue = 0;
                    this.data.ProgressBarColor = SliceModel.StateBrushes[4];
                });

                // Step 5: Add shells and infill
                Parallel.For(0, this.data.MaxSliceIdx + 1, opt, (i) => {
                    // Check for floor/roofs
                    var slice = this.SliceStore[i];

                    // Add shells
                    slice.AddShells(data.NumberOfShells, data.NozzleDiameter * dense_spacing);

                    // Add infill for surfaces
                    slice.AddDenseInfill(i % 2 == 0 ? surface_struct : surface_struct_alt);
                    slice.AddInfill(infill_struct);
              
                    // Reverse order polies
                    slice.SortPolygons();

                    // Add shapes
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        slice.ToShapes(bounds.X, bounds.Y, scale, this.data.PreviewArrowThickness, this.data.PreviewStrokeThickness);
                        this.data.SlicingProgressValue++;
                    });
                });



                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    this.Slice = this.SliceStore[0];
                    this.data.SliceShapes = this.Slice.Shapes;

                    this.data.SliceCanvas.Children.Clear();
                    this.data.SliceShapes.ForEach(x => this.data.SliceCanvas.Children.Add(x));

                    this.data.SlicingProgressValue = 0;
                    this.data.ProgressBarColor = SliceModel.StateBrushes[0];
                    this.data.SlicingInProgress = false;
                    this.data.CurrentSliceIdx = 0;
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

            return mesh;
        }
 

        private void BuildSliceBox()
        {
            // Slice box
            var meshBuilder = new MeshBuilder(false, false);

            // Add box
            var b = this.data.CurrentModel.Bounds;
            var offset = b.SizeX * 0.2;

            meshBuilder.AddBox(new Rect3D(
                b.Location.X - offset, b.Location.Y - offset, 
                b.Location.Z + this.data.CurrentSliceIdx * this.data.NozzleThickness, 
                b.SizeX + 2 * offset, b.SizeY + 2 * offset, 
                this.data.NozzleThickness
            ));


            var mesh = meshBuilder.ToMesh(false);
            var greenMaterial = MaterialHelper.CreateMaterial(Colors.LimeGreen, 50);
            var insideMaterial = MaterialHelper.CreateMaterial(Colors.Red);

            mesh.Normals = mesh.CalculateNormals();

            this.SlicePlane = new GeometryModel3D
            {
                Geometry = mesh,
                Material = greenMaterial,
                BackMaterial = insideMaterial,
            };

            this.data.CurrentSlicePlane = this.SlicePlane;
        }

        private void BuildSlice()
        {
            this.Original = SliceModel.GeometrizeModel(this.data.CurrentModel);
            var bounds = this.data.CurrentModel.Bounds;

            Construct obj = Construct.Create(this.Original, this.data.CurrentModel.Transform);
            //Construct box = Construct.Create(this.SlicePlane.Geometry as MeshGeometry3D);
            //Construct sli = obj.Intersect(box);

            var dense_spacing = this.data.NozzleDiameter * 2.875;  // Default == 1.15

            // Genarate infills
            var infill_struct = Polygon2D.GenerateInfill(
                bounds.X, bounds.Y,
                bounds.X + bounds.SizeX, bounds.Y + bounds.SizeY,
                this.data.NozzleDiameter, this.data.InfillSpacing, this.data.UseInfill
            );

            // Construct slice
            this.Slice = obj.Slice(bounds.Z + data.CurrentSliceIdx * data.NozzleThickness, 
                                   data.NozzleThickness);
            this.Slice.SetNozzleHeight(data.CurrentSliceIdx * data.NozzleThickness);
            this.Slice.Erode(data.NozzleThickness / 2.0);
            this.Slice.AddShells(data.NumberOfShells, data.NozzleThickness * dense_spacing);
            this.Slice.AddInfill(infill_struct);
            this.Slice.SortPolygons();

            var min = Math.Min(bounds.X, bounds.Y);
            var max = Math.Max(bounds.X + bounds.SizeX, bounds.Y + bounds.SizeY);
            var size = Math.Min(this.data.SliceCanvas.ActualWidth, this.data.SliceCanvas.ActualHeight);
            var scale = size / (max - min);

            this.data.SliceShapes = this.Slice.ToShapes(bounds.X, bounds.Y, scale, this.data.PreviewArrowThickness, this.data.PreviewStrokeThickness);

            this.data.SliceCanvas.Children.Clear();
            this.data.SliceShapes.ForEach(x => this.data.SliceCanvas.Children.Add(x));
        }
    }
}
