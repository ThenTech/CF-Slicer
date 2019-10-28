using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Media;

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

        int _valueSetting1 = 500;
        public int ValueSetting1
        {
            get { return _valueSetting1; }
            set
            {
                _valueSetting1 = value;
                OnPropertyChanged("ValueSetting1");
            }
        }

        int _MeshSizeU = 140;
        public int MeshSizeU
        {
            get { return _MeshSizeU; }
            set
            {
                _MeshSizeU = value;
                OnPropertyChanged("MeshSizeU");
            }
        }

        int _MeshSizeV = 140;
        public int MeshSizeV
        {
            get { return _MeshSizeV; }
            set
            {
                _MeshSizeV = value;
                OnPropertyChanged("MeshSizeV");
            }
        }

        double _ParameterW = 1;
        public double ParameterW
        {
            get { return _ParameterW; }
            set
            {
                _ParameterW = value;
                OnPropertyChanged("ParameterW");
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

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
