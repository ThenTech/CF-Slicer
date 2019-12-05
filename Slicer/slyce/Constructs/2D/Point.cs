using ClipperLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slicer.slyce.Constructs
{
    public class Point
    {
        // Conversion to ClipperLib.IntPoint
        public static readonly double INT_POINT_FACTOR = 1000.0;

        // Point equality tolerance
        public static readonly double EPSILON = 1e-6;

        public double X { get; set; }
        public double Y { get; set; }

        public Point(double X, double Y)
        {
            this.X = X;
            this.Y = Y;
        }

        public Point(IntPoint p)
        {
            this.X = (double)p.X / INT_POINT_FACTOR;
            this.Y = (double)p.Y / INT_POINT_FACTOR;
        }

        public IntPoint ToIntPoint()
        {
            return new IntPoint((long)(this.X * INT_POINT_FACTOR), (long)(this.Y * INT_POINT_FACTOR));
        }

        public System.Windows.Point ToWinPoint()
        {
            return new System.Windows.Point(this.X, this.Y);
        }

        public static System.Windows.Point IntToWinPoint(IntPoint p)
        {
            return new System.Windows.Point((double)p.X / INT_POINT_FACTOR, (double)p.Y / INT_POINT_FACTOR);
        }

        public static IntPoint WinToIntPoint(System.Windows.Point p)
        {
            return new IntPoint((long)(p.X * INT_POINT_FACTOR), (long)(p.Y * INT_POINT_FACTOR));
        }

        public override string ToString()
        {
            return "(" + X + ", " + Y + ")";
        }

        public bool Equals(Point p2)
        {
            return this.X.EpsilonEquals(p2.X, Point.EPSILON)
                && this.Y.EpsilonEquals(p2.Y, Point.EPSILON);
        }
    }
}
