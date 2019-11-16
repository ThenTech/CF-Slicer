using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shape = System.Windows.Shapes.Shape;

using Slicer.slyce.GCode;
using System.Windows.Media;

namespace Slicer.slyce.Constructs._2D
{
    public class Slice
    {
        public List<Polygon2D> Polygons { get; set; }
        public List<Shape> Shapes { get; set; }

        public double MinX { get; set; }
        public double MinY { get; set; }
        public double MaxX { get; set; }
        public double MaxY { get; set; }

        public double Z { get; set; }

        public Slice(List<Polygon2D> polies, double Z)
        {
            this.Polygons = polies;
            this.Z = Z;
        }

        public IEnumerable<Line> Lines
        {
            get
            {
                foreach (var p in this.Polygons)
                {
                    foreach (var l in p.Lines)
                    {
                        yield return l;
                    }
                }
            }
        }

        public List<Shape> ToShapes(double minX, double minY, double scale, double stroke = 1.0)
        {
            if (this.Shapes != null)
                return this.Shapes;

            this.MinX = minX;
            this.MinY = minY;

            this.Shapes = new List<Shape>();
            stroke = Math.Max(stroke / scale, 0.5);

            foreach (var l in this.Lines)
            {
                var line = new System.Windows.Shapes.Line
                {
                    Stroke = Brushes.Black,
                    X1 = (l.StartPoint.X - minX) * scale,
                    X2 = (l.EndPoint.X - minX) * scale,
                    Y1 = (l.StartPoint.Y - minY) * scale,
                    Y2 = (l.EndPoint.Y - minY) * scale,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                    VerticalAlignment = System.Windows.VerticalAlignment.Top,
                    StrokeThickness = stroke,
                    StrokeEndLineCap = PenLineCap.Round,
                    StrokeStartLineCap = PenLineCap.Round
                };

                this.Shapes.Add(line);
            }
            
            return this.Shapes;
        }
    }
}
