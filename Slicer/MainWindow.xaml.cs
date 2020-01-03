using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Resources;
using System.Windows.Shapes;
using HelixToolkit.Wpf;
using Microsoft.Win32;
using Slicer.GUI;
using Slicer.slyce.Constructs;
using Path = System.IO.Path;

namespace Slicer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ViewModel _viewModel = new ViewModel();

        public MainWindow()
        {
            InitializeComponent();

            // Change Culture, so numeric values use a dot instead of comma in a string...
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            
            DataContext = _viewModel;
            
            _viewModel.Brush = Brushes.Yellow;
            _viewModel.SliceCanvas = canvas_slice;
            _viewModel.NumberOfShells = 3;
            _viewModel.TextDimensionsColour = _viewModel.TextForegroundColour;
            _viewModel.PrinterCenter = new Point3D(_viewModel.PrinterDimX / 2, _viewModel.PrinterDimY / 2, 0);

            var test_location = "./../../../TestModels";
            if (Directory.Exists(test_location))
            {
                _viewModel.ModelFolder = Path.GetFullPath(test_location);
            }

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CommandBindings.Add(new CommandBinding(new RoutedCommand() {
                InputGestures = { new KeyGesture(Key.O, ModifierKeys.Control) }
            }, Open_Click));

            CommandBindings.Add(new CommandBinding(new RoutedCommand()
            {
                InputGestures = { new KeyGesture(Key.S, ModifierKeys.Control) }
            }, Export_Click));

            CommandBindings.Add(new CommandBinding(new RoutedCommand()
            {
                InputGestures = { new KeyGesture(Key.P, ModifierKeys.Control) }
            }, ExportGCode_Click));

            CommandBindings.Add(new CommandBinding(new RoutedCommand()
            {
                InputGestures = { new KeyGesture(Key.R, ModifierKeys.Control) }
            }, ResetCamera_Click));
            
            CommandBindings.Add(new CommandBinding(new RoutedCommand()
            {
                InputGestures = { new KeyGesture(Key.Z, ModifierKeys.Control) }
            }, Reset_Click));

            cboxInfillType.ItemsSource = Enum.GetValues(typeof(InfillType)).Cast<InfillType>();
            cboxInfillType.SelectedItem = this._viewModel.UseInfill;

            cboxSupportType.ItemsSource = Enum.GetValues(typeof(InfillType)).Cast<InfillType>();
            cboxSupportType.SelectedItem = this._viewModel.UseSupport;

            cboxAdhesionType.ItemsSource = Enum.GetValues(typeof(AdhesionType)).Cast<AdhesionType>();
            cboxAdhesionType.SelectedItem = this._viewModel.UseAdhesion;

            this.ResetCamera_Click(null, null);
            this.Reset_Click(null, null);
        }

        private void Load(string p)
        {
            ModelImporter import = new ModelImporter
            {
                DefaultMaterial = new DiffuseMaterial(_viewModel.Brush)
            };

            Model3D mod = null;

            try
            {
                mod = import.Load(p);
            } catch(Exception e) {
                MessageBox.Show("Cannot read model: " + e.ToString(), "Import error");
                return;
            }

            _viewModel.CurrentModel = mod;
            _viewModel.ModelTitle = Path.GetFileNameWithoutExtension(p);
            _viewModel.ModelFolder = Path.GetDirectoryName(p);

            _viewModel.Slicer = null;
            _viewModel.CurrentSlicePlane = null;
            _viewModel.CurrentSliceIdx = 0;
            _viewModel.SliceCanvas.Children.Clear();
            _viewModel.SlicingInProgress = false;

            // Update translation (set any property to update)
            _viewModel.ScaleX = 1.0;
            
            this.ResetCamera_Click(null, null);
            this.Reset_Click(null, null);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ViewSettings_Click(object sender, RoutedEventArgs e)
        {
            // todo: move to viewmodel
            if (ViewSettings.IsChecked)
            {
                panelSettings.Visibility = Visibility.Visible;
                Grid.SetColumn(viewport, 1);
                Grid.SetColumnSpan(viewport, 1);
            }
            else
            {
                panelSettings.Visibility = Visibility.Collapsed;
                Grid.SetColumn(viewport, 0);
                Grid.SetColumnSpan(viewport, 2);
            }
        }

        private void FullScreen_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Normal;
            WindowStyle = Fullscreen.IsChecked ? WindowStyle.None : WindowStyle.ThreeDBorderWindow;
            WindowState = Fullscreen.IsChecked ? WindowState.Maximized : WindowState.Normal;
            ResizeMode  = Fullscreen.IsChecked ? ResizeMode.NoResize : ResizeMode.CanResize;

            // mainMenu.Visibility = Fullscreen.IsChecked ? Visibility.Collapsed : Visibility.Visible;
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            var d = new SaveFileDialog
            {
                Filter = Exporters.Filter,
                DefaultExt = ".png",
                FileName = _viewModel.ModelTitle,
                Title = "Export current view",
                InitialDirectory = _viewModel.ModelFolder?.Length > 0
                                 ? _viewModel.ModelFolder
                                 : Environment.CurrentDirectory
            };

            if (d.ShowDialog().Value)
            {
                string ext = Path.GetExtension(d.FileName).ToLower();
                viewport.Export(d.FileName);
                Process.Start(Path.GetDirectoryName(d.FileName));
            }
        }

        private void ExportGCode_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.Slicer == null)
            {
                MessageBox.Show("Slice the model first!", "Export error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var d = new SaveFileDialog
            {
                Filter = "GCode|*.gcode",
                DefaultExt = ".gcode",
                FileName = _viewModel.ModelTitle,
                Title = "Save current slicing as gcode",
                InitialDirectory = _viewModel.ModelFolder?.Length > 0
                                 ? _viewModel.ModelFolder
                                 : Environment.CurrentDirectory
            };

            if (d.ShowDialog().Value)
            {
                slyce.GCode.GCodeWriter gcw = new slyce.GCode.GCodeWriter(
                    _viewModel.NozzleThickness, _viewModel.NozzleDiameter, _viewModel.FilamentDiameter
                //, _viewModel.UseAdhesion != AdhesionType.NONE  // TODO Optionally disable extra line if has adhesion
                );

                gcw.AddAllSlices(_viewModel.Slicer.SliceStore);
                gcw.ExportToFile(d.FileName);
            }
        }

        private void ResetCamera_Click(object sender, RoutedEventArgs e)
        {
            viewport.CameraController.ResetCamera();
            viewport.CameraController.ChangeDirection(new Vector3D(152, 263, -213), new Vector3D(0.287, 0.497, 0.819), 0);
            zoomBorder.Reset();
            zoomBorder.Fill();
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog() {
                Title = "Open a 3D model to start slicing...",
                CheckFileExists = true,
                Multiselect = false,
                Filter = "Model Files (*.obj;*.stl)|*.obj;*.stl|All files (*.*)|*.*",
                InitialDirectory = _viewModel.ModelFolder?.Length > 0 
                                 ? _viewModel.ModelFolder
                                 : Environment.CurrentDirectory
            };

            if (openFileDialog.ShowDialog() == true)
            {
                Console.WriteLine("Opened: " + openFileDialog.FileName);
                this.Load(openFileDialog.FileName);
            }
        }

        private void SliceDown_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.Slicer != null)
            {
                var newidx = _viewModel.CurrentSliceIdx - 1;
                _viewModel.CurrentSliceIdx = (newidx < 0) ? 0 : newidx;
            }
        }

        private void SliceUp_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.Slicer != null)
            {
                var newidx = _viewModel.CurrentSliceIdx + 1;
                _viewModel.CurrentSliceIdx = (newidx > _viewModel.MaxSliceIdx) ? _viewModel.MaxSliceIdx : newidx;
            }
        }

        private void Slice_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Start slicing...");
            
            _viewModel.Slicer = null;
            _viewModel.CurrentSliceIdx = 0;

            // Attempt cutting slice
            _viewModel.Slicer = new slyce.SliceModel(_viewModel);
            //_viewModel.Slicer.UpdateSlice();
            _viewModel.Slicer.BuildAllSlices();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ResetInProgress = true;
            _viewModel.ScaleX = 1.0;
            _viewModel.ScaleY = 1.0;
            _viewModel.ScaleZ = 1.0;
            _viewModel.RotationX = 0.0;
            _viewModel.RotationY = 0.0;

            // Set to false so next call will update viewmodel
            _viewModel.ResetInProgress = false;
            _viewModel.RotationZ = 0.0;
        }





        //private Point _mCurrentStart;
        //private Point _mPrevEnd;
        //private bool _isDragged;
        //private const double _scaleRate = 1.1;

        //protected void Canvas_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        //{
        //    base.OnMouseLeftButtonDown(e);
        //    CaptureMouse();

        //    if (e.OriginalSource is Border)
        //    {
        //        // Is Canvas?
        //        this._mCurrentStart = e.GetPosition(this);
        //        this._isDragged = true;
        //    }
        //}

        //protected void Canvas_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        //{
        //    base.OnMouseLeftButtonUp(e);
        //    ReleaseMouseCapture();

        //    if (this._isDragged)
        //    {
        //        this._isDragged = false;
        //        this._mPrevEnd = new Point(canvas_slice_transform.X, canvas_slice_transform.Y);
        //    }
        //}

        //protected void Canvas_OnMouseMove(object sender, MouseEventArgs e)
        //{
        //    if (!this._isDragged)
        //        return;

        //    base.OnMouseMove(e);
        //    if (e.LeftButton == MouseButtonState.Pressed && IsMouseCaptured)
        //    {
        //        var pos = e.GetPosition(this);
        //        var new_pos = new Point(_mPrevEnd.X + pos.X - this._mCurrentStart.X,
        //                                _mPrevEnd.Y + pos.Y - this._mCurrentStart.Y);

        //        canvas_slice_transform.X = new_pos.X;
        //        canvas_slice_transform.Y = new_pos.Y;
        //    }
        //}

        //protected void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        //{
        //    if (e.OriginalSource is Border)
        //    {
        //        var pos = e.GetPosition(this);
        //        canvas_slice_scale.CenterX = pos.X;
        //        canvas_slice_scale.CenterY = pos.Y;

        //        if (e.Delta > 0)
        //        {
        //            canvas_slice_scale.ScaleX *= _scaleRate;
        //            canvas_slice_scale.ScaleY *= _scaleRate;
        //        }
        //        else
        //        {
        //            canvas_slice_scale.ScaleX /= _scaleRate;
        //            canvas_slice_scale.ScaleY /= _scaleRate;
        //        }
        //    }
        //}
    }
}
