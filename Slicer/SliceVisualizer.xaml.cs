using Slicer.slyce.Constructs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Slicer
{
    /// <summary>
    /// Interaction logic for SliceVisualizer.xaml
    /// </summary>
    public partial class SliceVisualizer : Window
    {
        public static SliceVisualizer sliceVisualizer;
        private double scaleX;
        private double scaleY;
        private double minX;
        private double minY;
        private double maxX;
        private double maxY;
        public SliceVisualizer(Vertex[] vertices, double stroke, double minX, double minY, double maxX, double maxY)
        {
            InitializeComponent();
            Update(vertices, stroke, minX, minY, maxX, maxY);
        }

        public void Update(Vertex[] vertices, double stroke, double minX, double minY, double maxX, double maxY)
        {
            var width = canvasGrid.Width;
            var height = canvasGrid.Height;
            this.minX = minX;
            this.minY = minY;
            this.maxX = maxX;
            this.maxY = maxY;
            scaleX = width / (maxX - minX);
            scaleY = height / (maxY - minY);
            canvasGrid.Children.Clear();
            for (int i = 0; i < vertices.Count() - 1; i+=2)
            {
                var line = new Line();
                line.Stroke = System.Windows.Media.Brushes.Black;
                line.X1 = (vertices[i].Pos.X - minX) * scaleX;
                line.X2 = (vertices[i + 1].Pos.X - minX) * scaleX;
                line.Y1 = (vertices[i].Pos.Y - minY) * scaleY;
                line.Y2 = (vertices[i + 1].Pos.Y - minY) * scaleY;
                line.HorizontalAlignment = HorizontalAlignment.Left;
                line.VerticalAlignment = VerticalAlignment.Top;
                line.StrokeThickness = stroke * 10;
                canvasGrid.Children.Add(line);
            }
        }
    }
}
