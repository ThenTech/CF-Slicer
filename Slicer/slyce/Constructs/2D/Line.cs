using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slicer.slyce.Constructs._2D
{
    public class Line : IEquatable<Line>
    {
        public Point StartPoint { get; set; }
        public Point EndPoint { get; set; }

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
            return "(" + StartPoint + ")" + "-" + "(" + EndPoint + ")"; 
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
            return this.StartPoint.Equals(line.StartPoint) || this.StartPoint.Equals(line.EndPoint) || this.EndPoint.Equals(line.StartPoint) || this.EndPoint.Equals(line.EndPoint);
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
        
        public Line Reversed()
        {
            return new Line(EndPoint, StartPoint);
        }
    }
}
