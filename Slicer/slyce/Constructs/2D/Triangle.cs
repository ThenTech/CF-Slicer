using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slicer.slyce.Constructs._2D
{
    public class Triangle
    {
        public Point Point1 { get; set; }
        public Point Point2 { get; set; }
        public Point Point3 { get; set; }
        public Triangle(Point p1, Point p2, Point p3)
        {
            this.Point1 = p1;
            this.Point2 = p2;
            this.Point3 = p3;
        }
        public Triangle(double X1, double Y1, double X2, double Y2, double X3, double Y3)
        {
            this.Point1 = new Point(X1, Y1);
            this.Point2 = new Point(X2, Y2);
            this.Point3 = new Point(X3, Y3);
        }

        public void AddToPointList(List<Point> points)
        {
            points.Add(Point1);
            points.Add(Point2);
            points.Add(Point3);
        }
    }
}
