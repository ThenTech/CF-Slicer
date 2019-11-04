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
        private ModelVisual3D currentModel = new ModelVisual3D();
        

        public MainWindow()
        {
            InitializeComponent();

            // Change Culture, so numeric values use a dot instead of comma in a string...
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

            CompositionTarget.Rendering += this.OnCompositionTargetRendering;
            DataContext = _viewModel;

            _viewModel.PropertyChanged += ModelChanged;

            surface2 = new ClonedVisual3D();

            _viewModel.Brush = Brushes.Blue;

            //viewport.Viewport.Children.Add(new DefaultLights());

            v1 = viewport.Viewport;
            currentView = viewport;
            
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Load(path);
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

        private void Load(Uri uri)
        {
            try
            {
                StreamResourceInfo sri = Application.GetResourceStream(uri);
                Load(sri.Stream);
            }
            catch
            {
                MessageBox.Show("Cannot read model");
            }
        }

        private void Load(string p)
        {
            var s = new FileStream(p, FileMode.Open);
            Load(s);
            s.Close();
            _viewModel.ModelTitle = Path.GetFileNameWithoutExtension(p);
            _viewModel.ModelFolder = Path.GetDirectoryName(p);
        }

        private void Load(Stream s)
        {
            Invalidate();

            // todo: binding didn't work
            viewport.Title = _viewModel.ModelTitle;
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

        private void UpdateModel(string src)
        {
            //UpdateSurface(surface1, src);
            _isUpdating = false;
        }

        private void UpdateSurface(Mesh3D surface1, string src)
        {
            //surface1.Source = null;
            //surface1.MeshSizeU = _viewModel.MeshSizeU;
            //surface1.MeshSizeV = _viewModel.MeshSizeV;
            //surface1.ParameterW = _viewModel.ParameterW;

            //// now the surface should be updated
            //surface1.Source = src;
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
                InitialDirectory = _viewModel.ModelFolder
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
                InitialDirectory = _viewModel.ModelFolder
            };

            if (d.ShowDialog().Value)
            {
                Slicer.slyce.GCodeWriter gcw = new slyce.GCodeWriter();
                gcw.ExportToFile(d.FileName);
            }
        }

        private void ResetCamera_Click(object sender, RoutedEventArgs e)
        {
            viewport.CameraController.ResetCamera();
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = false;
            openFileDialog.Filter = "Model Files(*.obj;*.stl)|*.obj;*.stl|All files (*.*)|*.*";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filename in openFileDialog.FileNames)
                {
                    Console.Out.Write(filename);
                    ModelImporter import = new ModelImporter();
                    var mod = import.Load(filename);
                    currentModel = new ModelVisual3D();
                    currentModel.Content = mod;
                    viewport.Children.Add(currentModel);
                }
            }
        }
    }
}
