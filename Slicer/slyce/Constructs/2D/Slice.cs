using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shape = System.Windows.Shapes.Shape;

namespace Slicer.slyce.Constructs
{
    public class Slice
    {
        // Contains Outer Edges => the raw found slice lines connected
        public List<Polygon2D> Polygons { get; set; }

        // Should contain inner polygons, i.e. an outer poly subtracted by inner one
        // these should then be intersected with an infill structure (Clipper)
        // and the result would be a FillPolygons (only the segenmts required to print infill).
        // If a poly from Polygons.IsSurface, then a generated "surface" should also
        // be created and added to this list.
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
                        l.IsContour = p.IsContour;
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
                    poly.Offset(+delta, miter_limit);
                }
            }
        }

        public void AddShells(int nShells, double thickness)
        {
            // WARNING Does not take into account if shell poly intersects with other parts of the layer...
            foreach (var poly in this.Polygons)
            {
                // Note: inner most shells will be removed for infil, so add one more.
                for (int shell = 1; shell <= nShells /* -1 */; shell++)
                {
                    var contour = poly.Clone();
                    contour.Shell = shell;

                    if (poly.IsContour)
                    {
                        contour.Offset(-thickness * (double)shell, 10);
                    }
                    else
                    {
                        contour.Offset(+thickness * (double)shell, 10);
                    }

                    this.FillPolygons.Add(contour);
                }
            }
        }

        public void AddInfill(List<Polygon2D> infill_struct)
        {
            // Intersect infill_struct with contours and subtract holes from it.
            var infill = infill_struct.Select(p => p.Clone());

            var tmp_fill = new List<Polygon2D>();

            IEnumerable<Polygon2D> inner_shell = null;
            IEnumerable<Polygon2D> other_shell = null;

            if (this.FillPolygons.Count > 0)
            {
                // Has shell
                var most_inner_shell = this.FillPolygons.Max(p => p.Shell);
                inner_shell = this.FillPolygons.Where(p => p.Shell == most_inner_shell);
                other_shell = this.FillPolygons.Where(p => p.Shell < most_inner_shell);
            }
            else
            {
                inner_shell = this.Polygons;
                other_shell = new List<Polygon2D>();
            }

            // Intersect each poly from infill with each one of inner shells
            foreach (var inf in infill)
            {
                tmp_fill.AddRange(inf.Intersect(inner_shell));
            }

            this.FillPolygons = other_shell.ToList();
            //tmp_fill.AddRange(inner_shell);
            //tmp_fill = tmp_fill[0].Union(tmp_fill).ToList();
            //this.FillPolygons.AddRange(tmp_fill[0].Union(tmp_fill));

            this.FillPolygons.AddRange(tmp_fill);
            this.FillPolygons.ForEach(p => p.CleanLines());
        }

        public List<Shape> ToShapes(double minX, double minY, double scale, double arrow_scale = 1.0, double stroke = 1.0)
        {
            // Create Shapes for drawing the preview
            if (this.Shapes != null)
                return this.Shapes;

            this.MinX = minX;
            this.MinY = minY;

            this.Shapes = new List<Shape>();
            stroke = Math.Max(stroke / scale, stroke);

            // Add contours
            foreach (var l in this.Lines)
            {
                this.Shapes.Add(l.ToShape(minX, minY, scale, arrow_scale, stroke));
            }

            // Add fillers
            foreach (var l in this.LinesForFilling)
            {
                this.Shapes.Add(l.ToShape(minX, minY, scale, arrow_scale, stroke));
            }

            return this.Shapes;
        }
    }
}
