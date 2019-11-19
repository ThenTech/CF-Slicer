using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Slicer.slyce.Constructs._2D
{
    public class Line : IEquatable<Line>
    {
        public static readonly Brush BrushContour = Brushes.Black;
        public static readonly Brush BrushHole    = Brushes.Blue;
        public static readonly Brush BrushInfill  = Brushes.Red;

        public Point StartPoint { get; set; }
        public Point EndPoint { get; set; }

        public bool IsContour { get; set; } = true;
        public bool IsHole    { get => !IsContour; set => IsContour = !value; }
        public bool IsInfill  { get; set; } = false;

        public Line(double X1, double Y1, double X2, double Y2)
        {
            StartPoint = new Point(X1, Y1);
            EndPoint = new Point(X2, Y2);
        }

        public Line(Point p1, Point p2)
        {
            this.StartPoint = p1;
            this.EndPoint = p2;
        }

        public static Line ConvertToLine(Vertex v, Vertex w)
        {
            return new Line(v.Pos.X, v.Pos.Y, w.Pos.X, w.Pos.Y);
        }

        public void AddToPointList(List<Point> points)
        {
            points.Add(StartPoint);
            points.Add(EndPoint);
        }

        public override string ToString()
        {
            return StartPoint + " -> " + EndPoint;
        }

        public void Swap()
        {
            var tmp = StartPoint;
            StartPoint = EndPoint;
            EndPoint = tmp;
        }

        public bool Connects(Line l2)
        {
            return l2.StartPoint.Equals(this.EndPoint);
        }

        public bool CloseToConnects(Line l2)
        {
            if (EndPoint.X == l2.StartPoint.X && EndPoint.Y == l2.StartPoint.Y)
            {
                return true;
            }
            var xDiff = (EndPoint.X + 0.1) / (l2.StartPoint.X + 0.1);
            var yDiff = (EndPoint.Y + 0.1) / (l2.StartPoint.Y + 0.1);
            return xDiff <= 1.05 && yDiff <= 1.05 && xDiff >= 0.95 && yDiff >= 0.95;
        }

        public bool ReverseConnects(Line l2)
        {
            return l2.EndPoint.Equals(this.StartPoint);
        }

        public override bool Equals(object obj)
        {
            var l = (Line)obj;
            return l.StartPoint.Equals(this.StartPoint) && l.EndPoint.Equals(this.EndPoint);
        }

        public Line GetConnection(Line line2)
        {
            return new Line(this.EndPoint, line2.StartPoint);
        }

        public bool Equals(Line other)
        {
            return other != null &&
                   EqualityComparer<Point>.Default.Equals(this.StartPoint, other.StartPoint) &&
                   EqualityComparer<Point>.Default.Equals(this.EndPoint, other.EndPoint);
        }

        public bool CanConnect(Line line)
        {
            return this.StartPoint.Equals(line.StartPoint) 
                || this.StartPoint.Equals(line.EndPoint) 
                || this.EndPoint.Equals(line.StartPoint) 
                || this.EndPoint.Equals(line.EndPoint);
        }

        public override int GetHashCode()
        {
            var hashCode = 1140990155;
            hashCode = hashCode * -1521134295 + EqualityComparer<Point>.Default.GetHashCode(this.StartPoint);
            hashCode = hashCode * -1521134295 + EqualityComparer<Point>.Default.GetHashCode(this.EndPoint);
            return hashCode;
        }

        public static bool operator ==(Line line1, Line line2)
        {
            return EqualityComparer<Line>.Default.Equals(line1, line2);
        }

        public static bool operator !=(Line line1, Line line2)
        {
            return !(line1 == line2);
        }

        public double GetLength()
        {
            double x = this.StartPoint.X - this.EndPoint.X;
            double y = this.StartPoint.Y - this.EndPoint.Y;

            return Math.Sqrt(x * x + y * y);
        }

        public Line Reversed()
        {
            return new Line(EndPoint, StartPoint);
        }

        public System.Windows.Shapes.Shape ToShape(double minX, double minY, double scale, double arrow_scale, double stroke)
        {
            if (arrow_scale > 0.0)
            {
                // Line with arrow at end to give traverse/print direction
                return this.DrawLinkArrow(minX, minY, scale, stroke, arrow_scale,
                                          this.IsInfill ? Line.BrushInfill
                                                        : this.IsContour
                                                        ? Line.BrushContour : Line.BrushHole);
            }
            else
            {
                // Simple line
                return new System.Windows.Shapes.Line
                {
                    Stroke = this.IsInfill ? Line.BrushInfill
                                           : this.IsContour
                                           ? Line.BrushContour : Line.BrushHole,
                    X1 = (this.StartPoint.X - minX) * scale,
                    X2 = (this.EndPoint.X - minX) * scale,
                    Y1 = (this.StartPoint.Y - minY) * scale,
                    Y2 = (this.EndPoint.Y - minY) * scale,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                    VerticalAlignment = System.Windows.VerticalAlignment.Top,
                    StrokeThickness = stroke,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                };
            }
        }

        private System.Windows.Shapes.Shape DrawLinkArrow(double minX, double minY, double scale, double stroke, double arrow_scale, Brush brush)
        {
            System.Windows.Point p1 = new System.Windows.Point((this.StartPoint.X - minX) * scale,
                                                               (this.StartPoint.Y - minY) * scale);
            System.Windows.Point p2 = new System.Windows.Point((this.EndPoint.X - minX) * scale,
                                                               (this.EndPoint.Y - minY) * scale);

            double theta = Math.Atan2((p2.Y - p1.Y), (p2.X - p1.X)) * 180 / Math.PI;

            
            PathFigure pathFigure = new PathFigure
            {
                StartPoint = p2
            };

            System.Windows.Point lpoint = new System.Windows.Point(p2.X + 2.1 * arrow_scale, p2.Y + 5.2 * arrow_scale);
            System.Windows.Point rpoint = new System.Windows.Point(p2.X - 2.1 * arrow_scale, p2.Y + 5.2 * arrow_scale);

            pathFigure.Segments.Add(new LineSegment
            {
                Point = lpoint
            });
            pathFigure.Segments.Add(new LineSegment
            {
                Point = rpoint
            });
            pathFigure.Segments.Add(new LineSegment
            {
                Point = p2
            });

            PathGeometry pathGeometry = new PathGeometry();
            pathGeometry.Figures.Add(pathFigure);
            pathGeometry.Transform = new RotateTransform
            {
                Angle = theta + 90,
                CenterX = p2.X,
                CenterY = p2.Y

            };

            GeometryGroup lineGroup = new GeometryGroup();
            lineGroup.Children.Add(pathGeometry);
            lineGroup.Children.Add(new LineGeometry
            {
                StartPoint = p1,
                EndPoint = p2
            });

            return new System.Windows.Shapes.Path
            {
                Data = lineGroup,
                StrokeThickness = stroke,
                Stroke = brush,
                Fill = brush,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
            };
        }
    }
}
