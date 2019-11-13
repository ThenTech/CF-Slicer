using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slicer.slyce.Constructs._2D
{
    public class Point
    {
        public double X { get; set; }
        public double Y { get; set; }
        public Point(double X, double Y)
        {
            this.X = X;
            this.Y = Y;
        }
        public override string ToString()
        {
            return X + "," + Y;
        }
        public bool Equals(Point p2)
        {
            return this.X == p2.X && this.Y == p2.Y;
        }
    }
}
