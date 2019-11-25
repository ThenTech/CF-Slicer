using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Controls;
using System.Windows;
using Slicer.slyce.Constructs;

namespace Slicer.GUI
{
    public class ViewModel : INotifyPropertyChanged
    {
        public SolidColorBrush TextForegroundColour { get; } = new SolidColorBrush(Color.FromRgb(0xF0, 0xF1, 0xF2));
        public SolidColorBrush TextErrorColour      { get; } = new SolidColorBrush(Color.FromRgb(0xFF, 0x20, 0x20));
        public SolidColorBrush TextWarningColour    { get; } = new SolidColorBrush(Color.FromRgb(0xF7, 0x77, 0x00));

        public Brush TextDimensionsColour { get; set; }

        Brush _Brush;
        public Brush Brush
        {
            get { return _Brush; }
            set
            {
                _Brush = value;
                OnPropertyChanged("Brush");
            }
        }

        public bool ResetInProgress { get; set; } = false;

        public double DimX { get => HasModel ? this.CurrentModel.Bounds.SizeX : 0.0; }
        public double DimY { get => HasModel ? this.CurrentModel.Bounds.SizeY : 0.0; }
        public double DimZ { get => HasModel ? this.CurrentModel.Bounds.SizeZ : 0.0; }

        public double PrinterDimX { get => 220; }
        public double PrinterDimY { get => 220; }
        public double PrinterDimZ { get => 250; }

        private Point3D _PrinterCenter;
        public Point3D PrinterCenter {
            get {
                return _PrinterCenter;
            }
            set {
                _PrinterCenter = value;
                OnPropertyChanged("PrinterCenter");
            }
        }

        double _ScaleX = 1;
        public double ScaleX
        {
            get { return _ScaleX; }
            set
            {
                _ScaleX = value;
                OnPropertyChanged("ScaleX");
            }
        }

        double _ScaleY = 1;
        public double ScaleY
        {
            get { return _ScaleY; }
            set
            {
                _ScaleY = value;
                OnPropertyChanged("ScaleY");
            }
        }

        double _ScaleZ = 1;
        public double ScaleZ
        {
            get { return _ScaleZ; }
            set
            {
                _ScaleZ = value;
                OnPropertyChanged("ScaleZ");
            }
        }

        double _RotationX = 0;
        public double RotationX
        {
            get { return _RotationX; }
            set
            {
                _RotationX = value;
                OnPropertyChanged("RotationX");
            }
        }

        double _RotationY = 0;
        public double RotationY
        {
            get { return _RotationY; }
            set
            {
                _RotationY = value;
                OnPropertyChanged("RotationY");
            }
        }

        double _RotationZ = 0;
        public double RotationZ
        {
            get { return _RotationZ; }
            set
            {
                _RotationZ = value;
                OnPropertyChanged("RotationZ");
            }
        }

        double _NozzleThickness = 0.20;
        public double NozzleThickness
        {
            get { return _NozzleThickness; }
            set
            {
                _NozzleThickness = value;
                OnPropertyChanged("NozzleThickness");
            }
        }

        double _NozzleDiameter = 0.4;
        public double NozzleDiameter
        {
            get { return _NozzleDiameter; }
            set
            {
                _NozzleDiameter = value;
                OnPropertyChanged("NozzleDiameter");
            }
        }

        double _FilamentDiameter = 1.75;    // 2.6477
        public double FilamentDiameter
        {
            get { return _FilamentDiameter; }
            set
            {
                _FilamentDiameter = value;
                OnPropertyChanged("FilamentDiameter");
            }
        }
        
        int _NumberOfShells = 3;
        public int NumberOfShells
        {
            get { return _NumberOfShells; }
            set
            {
                _NumberOfShells = value;
                OnPropertyChanged("NumberOfShells");
            }
        }
                
        double _ShellThickness = 0;
        public double ShellThickness
        {
            get { return _ShellThickness; }
            set
            {
                _ShellThickness = value;
                OnPropertyChanged("ShellThickness");
            }
        }

        string _ModelTitle;
        public string ModelTitle
        {
            get { return _ModelTitle; }
            set
            {
                _ModelTitle = value;
                OnPropertyChanged("ModelTitle");
            }
        }

        string _ModelFolder;
        public string ModelFolder
        {
            get { return _ModelFolder; }
            set
            {
                _ModelFolder = value;
                OnPropertyChanged("ModelFolder");
            }
        }

        private Model3D _CurrentModel;
        public Model3D CurrentModel
        {
            get { return _CurrentModel; }
            set {
                _CurrentModel = value;
                OnPropertyChanged("CurrentModel");
            }
        }

        private bool _HasModel;
        public bool HasModel
        {
            get { return _HasModel; }
            set
            {
                _HasModel = value;
                OnPropertyChanged("HasModel");
            }
        }

        private Model3D _CurrentSlicePlane;
        public Model3D CurrentSlicePlane
        {
            get { return _CurrentSlicePlane; }
            set
            {
                _CurrentSlicePlane = value;
                OnPropertyChanged("CurrentSlicePlane");
            }
        }

        private int _CurrentSliceIdx = 0;
        public int CurrentSliceIdx
        {
            get { return _CurrentSliceIdx; }
            set
            {
                _CurrentSliceIdx = value;
                OnPropertyChanged("CurrentSliceIdx");
            }
        }

        private int _MaxSliceIdx = 0;
        public int MaxSliceIdx
        {
            get { return _MaxSliceIdx; }
            set
            {
                _MaxSliceIdx = value;
                OnPropertyChanged("MaxSliceIdx");
            }
        }

        private slyce.SliceModel _Slicer = null;
        public slyce.SliceModel Slicer
        {
            get { return _Slicer; }
            set
            {
                _Slicer = value;
                OnPropertyChanged("Slicer");
            }
        }

        private List<System.Windows.Shapes.Shape> _SliceShapes = null;
        public List<System.Windows.Shapes.Shape> SliceShapes
        {
            get { return _SliceShapes; }
            set
            {
                _SliceShapes = value;
                OnPropertyChanged("SliceShapes");
            }
        }

        private Canvas _SliceCanvas = null;
        public Canvas SliceCanvas
        {
            get { return _SliceCanvas; }
            set
            {
                _SliceCanvas = value;
                OnPropertyChanged("SliceCanvas");
            }
        }

        private bool _SlicingInProgress = false;
        public bool SlicingInProgress {
            get { return _SlicingInProgress; }
            set
            {
                _SlicingInProgress = value;
                OnPropertyChanged("SlicingInProgress");
                OnPropertyChanged("SlicingProgressVisible");
            }
        }

        public Visibility SlicingProgressVisible {
            get => SlicingInProgress ? Visibility.Visible : Visibility.Hidden;
        }

        private int _SlicingProgressValue = 0;
        public int SlicingProgressValue {
            get { return _SlicingProgressValue; }
            set
            {
                _SlicingProgressValue = value;
                OnPropertyChanged("SlicingProgressValue");
            }
        }

        private double _PreviewStrokeThickness = 0.5;
        public double PreviewStrokeThickness {
            get { return _PreviewStrokeThickness; }
            set
            {
                _PreviewStrokeThickness = value;
                OnPropertyChanged("PreviewStrokeThickness");
            }
        }

        private double _PreviewArrowThickness = 1.0;
        public double PreviewArrowThickness {
            get { return _PreviewArrowThickness; }
            set
            {
                _PreviewArrowThickness = value;
                OnPropertyChanged("PreviewStrokeThickness");
            }
        }

        public InfillType UseInfill { get; set; } = InfillType.SQUARE;

        private double _InfillSpacing = 7;
        public double InfillSpacing {
            get { return _InfillSpacing; }
            set
            {
                _InfillSpacing = value;
                OnPropertyChanged("InfillSpacing");
            }
        } 

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
            if (propertyName == "CurrentModel")
            {
                HasModel = CurrentModel != null;
                OnPropertyChanged("DimX");
                OnPropertyChanged("DimY");
                OnPropertyChanged("DimZ");
            }
            else if (propertyName == "PrinterCenter")
            {
                OnPropertyChanged("PrinterDimX");
                OnPropertyChanged("PrinterDimY");
                OnPropertyChanged("PrinterDimZ");
            }
            else if (HasModel && !ResetInProgress
                && (propertyName == "ScaleX" || propertyName == "ScaleY" || propertyName == "ScaleZ"
                 || propertyName == "RotationX" || propertyName == "RotationY" || propertyName == "RotationZ"
                 || propertyName == "PositionX" || propertyName == "PositionY" || propertyName == "PositionZ"
                ))
            {
                Transform3DGroup combined = new Transform3DGroup();

                // Scale
                combined.Children.Add(new ScaleTransform3D
                {
                    ScaleX = ScaleX,
                    ScaleY = ScaleY,
                    ScaleZ = ScaleZ,
                });

                // Rotate X
                combined.Children.Add(new RotateTransform3D()
                {
                    Rotation = new AxisAngleRotation3D()
                    {
                        Axis = new Vector3D(1, 0, 0),
                        Angle = RotationX * 360.0
                    }
                });

                // Rotate Y
                combined.Children.Add(new RotateTransform3D()
                {
                    Rotation = new AxisAngleRotation3D()
                    {
                        Axis = new Vector3D(0, 1, 0),
                        Angle = RotationY * 360.0
                    }
                });
                
                // Rotate Z
                combined.Children.Add(new RotateTransform3D()
                {
                    Rotation = new AxisAngleRotation3D()
                    {
                        Axis = new Vector3D(0, 0, 1),
                        Angle = RotationZ * 360.0
                    }
                });

                CurrentModel.Transform = combined;

                // Refit onto printing plane
                var b = CurrentModel.Bounds;

                // Translate (is done first)
                combined.Children.Add(new TranslateTransform3D()
                {
                    OffsetX = (PrinterDimX - (b.SizeX)) / 2.0 - b.X,
                    OffsetY = (PrinterDimY - (b.SizeY)) / 2.0 - b.Y,
                    OffsetZ = 0 - b.Z,
                });

                CurrentModel.Transform = combined;
                OnPropertyChanged("DimX");
                OnPropertyChanged("DimY");
                OnPropertyChanged("DimZ");

                CurrentSliceIdx = 0;
            }
            else if (propertyName == "NozzleThickness")
            {
                CurrentSliceIdx = 0;
            }
            else if (HasModel
                && (propertyName == "CurrentSliceIdx" || propertyName == "NozzleThickness"))
            {

                // Set amount of slice layers
                var height = CurrentModel.Bounds.SizeZ;
                MaxSliceIdx = (int)Math.Floor(height / NozzleThickness);

                if (Slicer != null)
                {
                    Slicer.UpdateSlice();
                }
            }
            else if (propertyName == "NumberOfShells")
            {
                _NumberOfShells = Math.Max(1, NumberOfShells);
                ShellThickness = NozzleDiameter * NumberOfShells;
            }
            else if (propertyName == "PreviewStrokeThickness" || propertyName == "PreviewArrowThickness")
            {
                if (Slicer != null)
                {
                    Slicer.RedrawAllSlices();
                }
            }
            else if (propertyName == "DimX" || propertyName == "DimY" || propertyName == "DimZ")
            {
                if (DimX >= PrinterDimX || DimY >= PrinterDimY || DimZ >= PrinterDimZ)
                {
                    TextDimensionsColour = this.TextErrorColour;
                }
                else
                {
                    TextDimensionsColour = this.TextForegroundColour;
                }

                OnPropertyChanged("TextDimensionsColour");
            }
        }                              
                                     
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
