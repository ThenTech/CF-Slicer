using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Slicer.GUI
{
    public class ViewModel : INotifyPropertyChanged
    {
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

        double _PositionX = 0;
        public double PositionX
        {
            get { return _PositionX; }
            set
            {
                _PositionX = value;
                OnPropertyChanged("PositionX");
            }
        }

        double _PositionY = 0;
        public double PositionY
        {
            get { return _PositionY; }
            set
            {
                _PositionY = value;
                OnPropertyChanged("PositionY");
            }
        }

        double _PositionZ = 0;
        public double PositionZ
        {
            get { return _PositionZ; }
            set
            {
                _PositionZ = value;
                OnPropertyChanged("PositionZ");
            }
        }

        double _NozzleThickness = 0.22;
        public double NozzleThickness
        {
            get { return _NozzleThickness; }
            set
            {
                _NozzleThickness = value;
                OnPropertyChanged("NozzleThickness");
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

        private Model3D _CurrentSlice;
        public Model3D CurrentSlice
        {
            get { return _CurrentSlice; }
            set {
                _CurrentSlice = value;
                OnPropertyChanged("CurrentSlice");
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
            }
            else if (HasModel
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

                // Translate (is done first)
                combined.Children.Add(new TranslateTransform3D()
                {
                    OffsetX = PositionX,
                    OffsetY = PositionY,
                    OffsetZ = PositionZ
                });

                CurrentModel.Transform = combined;

                CurrentSliceIdx = 0;
            }
            else if (HasModel
                && (propertyName == "CurrentSliceIdx" || propertyName == "NozzleThickness"))
            {
                // Set amount of slice layers
                var height = CurrentModel.Bounds.SizeZ;
                MaxSliceIdx = (int)Math.Ceiling(height / NozzleThickness);

                if (Slicer != null)
                {
                    Slicer.UpdateSlice();

                    // Temp to show movement, but Slicer.Sliced itself should move when changing CurrentSliceIdx
                    CurrentSlice.Transform = new TranslateTransform3D()
                    {
                        OffsetZ = CurrentSliceIdx * NozzleThickness
                    };
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
