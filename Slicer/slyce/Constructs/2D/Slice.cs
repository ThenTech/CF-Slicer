using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Slicer.slyce.GCode;

namespace Slicer.slyce.Constructs._2D
{
    public class Slice
    {
        public List<Polygon2D> Polygons { get; set; }

        public List<Triangle> TrianglesInSlice { get; set; }
        public List<Line> Lines { get; set; }

        public double MinX { get; set; }
        public double MinY { get; set; }
        public double MaxX { get; set; }
        public double MaxY { get; set; }

        public double Z { get; set; }

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

        public Slice(List<Polygon2D> polies, double MinX, double MinY, double MaxX, double MaxY)
        {
            this.Polygons = polies;
            this.Lines = new List<Line>();
            this.TrianglesInSlice = new List<Triangle>();
            this.MinX = MinX;
            this.MinY = MinY;
            this.MaxX = MaxX;
            this.MaxY = MaxY;

            foreach (var p in this.Polygons)
            {
                foreach (var line in p.Lines)
                {
                    this.Lines.Add(line);
                }
            }
        }
    }
}
