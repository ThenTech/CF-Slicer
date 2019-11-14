using Slicer.slyce.Constructs;
using Slicer.slyce.Constructs._2D;
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
        private double scale;
        private double min;
        private double max;
        private double stroke;
        private Slice slice;

        public SliceVisualizer()
        {
            InitializeComponent();
        }

        public SliceVisualizer(Slice slice, double stroke)
        {
            InitializeComponent();
            this.slice = slice;
            this.stroke = stroke;   
        }

        public void Init()
        {
            RecalculateMinMax(slice);
            Update(slice, stroke);
        }

        public void RecalculateMinMax(Slice slice)
        {
            this.min = slice.MinX;
            if (slice.MinY < min)
            {
                min = slice.MinY;
            }
            this.max = slice.MaxX;
            if (slice.MaxY > max)
            {
                max = slice.MaxY;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ClearValue(SizeToContentProperty);
            LayoutRoot.ClearValue(WidthProperty);
            LayoutRoot.ClearValue(HeightProperty);
        }

        public void DrawPolygon(List<slyce.Constructs._2D.Point> allPoints, double stroke, double min, double max)
        {
            this.Title = "DRAW POLY";
            var width = canvasGrid.Width;
            var height = canvasGrid.Height;
            var comp = width;

            if (height < comp)
            {
                comp = height;
            }

            scale = comp / (max - min);
            System.Windows.Shapes.Polygon myPolygon = new System.Windows.Shapes.Polygon();
            PointCollection points = new PointCollection();

            foreach (var p in allPoints)
            {
                points.Add(new System.Windows.Point((p.X - min)*scale, (p.Y - min)*scale));
            }

            //Color c = Color.FromRgb((byte)r.Next(255), (byte)r.Next(255), (byte)r.Next(255));
            myPolygon.Fill = Brushes.Black;
            myPolygon.Stroke = Brushes.Black;
            myPolygon.StrokeThickness = stroke;
            myPolygon.HorizontalAlignment = HorizontalAlignment.Left;
            myPolygon.VerticalAlignment = VerticalAlignment.Top;
            myPolygon.Points = points;
            canvasGrid.Children.Add(myPolygon);
        }

        public void Update(Slice slice, double stroke)
        {
            var width = canvasGrid.Width;
            var height = canvasGrid.Height;
            var comp = width;

            if(height < comp)
            {
                comp = height;
            }

            scale = comp / (max - min);
            canvasGrid.Children.Clear();
            //Random r = new Random();

            foreach (var t in slice.TrianglesInSlice)
            {
                System.Windows.Shapes.Polygon myPolygon = new System.Windows.Shapes.Polygon();

                PointCollection points = new PointCollection();
                points.Add(new System.Windows.Point((t.Point1.X - min)*scale, (t.Point1.Y - min) * scale));
                points.Add(new System.Windows.Point((t.Point2.X - min) * scale, (t.Point2.Y - min) * scale));
                points.Add(new System.Windows.Point((t.Point3.X - min) * scale, (t.Point3.Y - min) * scale));
                //Color c = Color.FromRgb((byte)r.Next(255), (byte)r.Next(255), (byte)r.Next(255));

                myPolygon.Fill = Brushes.Black;
                myPolygon.Stroke = Brushes.Black;
                myPolygon.StrokeThickness = stroke;
                myPolygon.HorizontalAlignment = HorizontalAlignment.Left;
                myPolygon.VerticalAlignment = VerticalAlignment.Top;
                myPolygon.Points = points;

                canvasGrid.Children.Add(myPolygon);
            }

            foreach (var l in slice.Lines)
            {
                var line = new System.Windows.Shapes.Line();

                line.Stroke = System.Windows.Media.Brushes.Black;
                line.X1 = (l.StartPoint.X - min) * scale;
                line.X2 = (l.EndPoint.X - min) * scale;
                line.Y1 = (l.StartPoint.Y - min) * scale;
                line.Y2 = (l.EndPoint.Y - min) * scale;
                line.HorizontalAlignment = HorizontalAlignment.Left;
                line.VerticalAlignment = VerticalAlignment.Top;
                line.StrokeThickness = stroke; //* scale;

                canvasGrid.Children.Add(line);
            }
        }
    }
}
