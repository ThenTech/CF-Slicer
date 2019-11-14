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
using Path = System.IO.Path;

namespace Slicer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ViewModel _viewModel = new ViewModel();

        private readonly ClonedVisual3D surface2;
        private bool _isInvalidated = true;
        private bool _isUpdating;
        private object updateLock = "abc";
        private UIElement currentView;
        private Viewport3D v1;
        private Viewport3D v2;
        private SliceVisualizer sliceVisualizer;

        public MainWindow()
        {
            InitializeComponent();

            // Change Culture, so numeric values use a dot instead of comma in a string...
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

            CompositionTarget.Rendering += this.OnCompositionTargetRendering;
            DataContext = _viewModel;

            _viewModel.PropertyChanged += ModelChanged;

            surface2 = new ClonedVisual3D();

            _viewModel.Brush = Brushes.Yellow;

            v1 = viewport.Viewport;
            currentView = viewport;
            v2 = viewport_slice.Viewport;

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void OnCompositionTargetRendering(object sender, EventArgs e)
        {
            if (_isInvalidated)
            {
                // sync:
                // _isInvalidated = false;
                // UpdateModel(source1.Text);

                // async:
                BeginUpdateModel();
            }
        }

        private void ModelChanged(object sender, PropertyChangedEventArgs e)
        {
            Invalidate();
        }

        private void Load(string p)
        {
            Invalidate();

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
            _viewModel.CurrentSlice = null;
            _viewModel.ModelTitle = Path.GetFileNameWithoutExtension(p);
            _viewModel.ModelFolder = Path.GetDirectoryName(p);

            _viewModel.Slicer = null;
            _viewModel.CurrentSlicePlane = null;
            _viewModel.CurrentSliceIdx = 0;

            // Update translation (set any property to update)
            _viewModel.ScaleX = 1.0;
            
            viewport.CameraController.ResetCamera();
        }

        private void Invalidate()
        {
            lock (updateLock)
            {
                _isInvalidated = true;
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.sliceVisualizer.Close();
            Close();
        }

        private void BeginUpdateModel()
        {
            lock (updateLock)
            {
                if (!_isUpdating)
                {
                    _isInvalidated = false;
                    _isUpdating = true;
                    //Dispatcher.Invoke(new Action<string>(UpdateModel), src);
                }
            }
        }

        private void ViewSettings_Click(object sender, RoutedEventArgs e)
        {
            // todo: move to viewmodel
            if (ViewSettings.IsChecked)
            {
                panelSettings.Visibility = Visibility.Visible;
                Grid.SetColumn(viewport, 1);
                Grid.SetColumnSpan(viewport, 1);
                Grid.SetColumn(viewport_slice, 1);
                Grid.SetColumnSpan(viewport_slice, 1);
                Grid.SetColumn(viewportsplitter, 1);
                Grid.SetColumnSpan(viewportsplitter, 1);
            }
            else
            {
                panelSettings.Visibility = Visibility.Collapsed;
                Grid.SetColumn(viewport, 0);
                Grid.SetColumnSpan(viewport, 2);
                Grid.SetColumn(viewport_slice, 0);
                Grid.SetColumnSpan(viewport_slice, 2);
                Grid.SetColumn(viewportsplitter, 0);
                Grid.SetColumnSpan(viewportsplitter, 2);
            }
        }

        private void FullScreen_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Normal;
            WindowStyle = Fullscreen.IsChecked ? WindowStyle.None : WindowStyle.ThreeDBorderWindow;
            WindowState = Fullscreen.IsChecked ? WindowState.Maximized : WindowState.Normal;
            ResizeMode = Fullscreen.IsChecked ? ResizeMode.NoResize : ResizeMode.CanResize;

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
                slyce.GCode.GCodeWriter gcw = new slyce.GCode.GCodeWriter();
                gcw.ExportToFile(d.FileName);
            }
        }

        private void ResetCamera_Click(object sender, RoutedEventArgs e)
        {
            viewport.CameraController.ResetCamera();
            viewport_slice.CameraController.ResetCamera();
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

        private void Slice_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Start slicing...");
            
            _viewModel.Slicer = null;
            _viewModel.CurrentSliceIdx = 0;

            // Attempt cutting slice
            _viewModel.Slicer = new slyce.SliceModel(_viewModel);
            _viewModel.Slicer.UpdateSlice();

            if(sliceVisualizer == null)
            {
                sliceVisualizer = new SliceVisualizer(_viewModel.Slicer.Slice, 1);
                sliceVisualizer.Show();
                sliceVisualizer.Init();
                _viewModel.sliceVisualizer = sliceVisualizer;
            }
            else
            {
                sliceVisualizer.RecalculateMinMax(_viewModel.Slicer.Slice);
                sliceVisualizer.Update(_viewModel.Slicer.Slice, 1);
            }
            //var slicegroup = new Model3DGroup();
            ////slicegroup.Children.Add(_viewModel.Slicer.SlicePlane);
            //slicegroup.Children.Add(_viewModel.Slicer.Sliced);
            //_viewModel.CurrentSlice = slicegroup;
        }
    }
}
