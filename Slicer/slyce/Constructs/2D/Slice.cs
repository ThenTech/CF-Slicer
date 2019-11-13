using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slicer.slyce.Constructs._2D
{
    public class Slice
    {
        public List<Triangle> TrianglesInSlice { get; set; }
        public List<Line> Lines { get; set; }
        public double MinX { get; set; }
        public double MinY { get; set; }
        public double MaxX { get; set; }
        public double MaxY { get; set; }
        public Slice(List<Line> lines, List<Triangle> triangles)
        {
            List<Point> allPoints = new List<Point>();
            this.Lines = lines;
            this.TrianglesInSlice = triangles;
            foreach (var l in Lines)
            {
                l.AddToPointList(allPoints);
            }
            foreach (var t in TrianglesInSlice)
            {
                t.AddToPointList(allPoints);
            }
            if(allPoints.Count() > 0)
            {
                MinX = allPoints.Min(p => p.X);
                MinY = allPoints.Min(p => p.Y);
                MaxX = allPoints.Max(p => p.X);
                MaxY = allPoints.Max(p => p.Y);
            }
            
        }
        public Slice(List<Line> lines, List<Triangle> triangles, double MinX, double MinY, double MaxX, double MaxY)
        {
            this.Lines = lines;
            this.TrianglesInSlice = triangles;
            this.MinX = MinX;
            this.MinY = MinY;
            this.MaxX = MaxX;
            this.MaxY = MaxY;
        }
    }
}
