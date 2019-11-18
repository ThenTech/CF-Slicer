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
        // Contains Outer Edges => the raw found slice lines connected
        public List<Polygon2D> Polygons { get; set; }

        // Should contain inner polygons, i.e. an outer poly subtracted by inner one
        // these should then be intersected with an infill structure (Clipper)
        // and the result would be a FillPolygons (only the segenmts required to print infill).
        // If a poly from Polygons.IsSurface, then a generated "surface" should also
        // be created and added ti this list.
        public List<Polygon2D> FillPolygons { get; set; }

        public List<Shape> Shapes { get; set; }

        public IEnumerable<Line> Lines
        {
            get
            {
                foreach (var p in this.Polygons)
                {
                    foreach (var l in p.Lines)
                    {
                        l.IsContour = true;
                        yield return l;
                    }
                }
            }
        }

        public IEnumerable<Line> LinesForFilling
        {
            get
            {
                foreach (var p in this.FillPolygons)
                {
                    foreach (var l in p.Lines)
                    {
                        l.IsInfill = true;
                        yield return l;
                    }
                }
            }
        }

        public double MinX { get; set; }
        public double MinY { get; set; }
        public double MaxX { get; set; }
        public double MaxY { get; set; }

        public double Z { get; set; }
        public double ZHeight { get; set; }

        public Slice(List<Polygon2D> polies, double Z)
        {
            this.Polygons = polies;
            this.FillPolygons = new List<Polygon2D>();
            this.Z = Z;
            this.ZHeight = Z;
        }

        public void SetNozzleHeight(double height)
        {
            this.ZHeight = height;
        }

        public void Erode(double delta, double miter_limit = 5)
        {
            foreach (var poly in this.Polygons)
            {
                if (poly.IsContour)
                {
                    // Expand inwards
                    poly.Offset(-delta, miter_limit);
                }
                else // if (poly.IsHole)
                {
                    // Expand outwards == inwards into object
                    poly.Offset(delta, miter_limit);
                }
            }
        }

        public List<Shape> ToShapes(double minX, double minY, double scale, double stroke = 1.0)
        {
            // Create Shapes for drawing the preview
            if (this.Shapes != null)
                return this.Shapes;

            this.MinX = minX;
            this.MinY = minY;

            this.Shapes = new List<Shape>();
            stroke = Math.Max(stroke / scale, 0.5);

            // Add contours
            foreach (var l in this.Lines)
            {
                this.Shapes.Add(l.ToShape(minX, minY, scale, stroke));
            }

            // Add fillers
            foreach (var l in this.LinesForFilling)
            {
                this.Shapes.Add(l.ToShape(minX, minY, scale, stroke));
            }

            return this.Shapes;
        }
    }
}
