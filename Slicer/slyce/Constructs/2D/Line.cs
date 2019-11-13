using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slicer.slyce.Constructs._2D
{
    public class Line
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
    }
}
