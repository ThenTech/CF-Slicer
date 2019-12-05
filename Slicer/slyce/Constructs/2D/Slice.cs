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
        public bool HasSurface { get; set; }

        public double Z { get; set; }
        public double ZHeight { get; set; }

        public Slice(List<Polygon2D> polies, double Z, bool hasSurface)
        {
            this.Polygons = polies;
            this.FillPolygons = new List<Polygon2D>();
            this.Z = Z;
            this.ZHeight = Z;
            this.HasSurface = hasSurface;

            // TODO Selective?
            if (this.HasSurface)
            {
                //this.Polygons.ForEach(p => p.IsSurface = true);
            }
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

        public void DetermineSurfaces(Slice above, Slice below)
        {
            // Subtract above/below/this.Polygons


            // Add newly found polies to FillPolygons, and set IsSurface to them.
        }

        public void AddShells(int nShells, double thickness)
        {
            // WARNING Does not take into account if shell poly intersects with other parts of the layer...
            // EDIT Now ignores shells that would overlap

            var shell_miter = 10.0;

            foreach (var poly in this.Polygons)
            {
                Polygon2D inner_most = null;

                // Note: inner most shells will be removed for infil, so add one more.
                for (int shell = 1; shell < nShells /* -1 */; shell++)
                {
                    var contour = poly.Clone();
                    contour.Shell = shell;
                    contour.IsInfill = contour.IsShell = true;

                    if (poly.IsContour)
                    {
                        contour.Offset(-thickness * (double)shell, shell_miter);

                        // TODO Always add but clip with existing things?
                        //foreach (var p in this.Polygons)
                        //{
                        //    var result = p.Intersects(contour);
                        //    if (result.Item1)
                        //    {
                        //        var res = result.Item2.FirstOrDefault();
                        //        if (res != null)
                        //        {
                        //            contour = res.Union(result.Item2).First();
                        //        }
                        //    }
                        //}

                        //this.FillPolygons.Add(contour);


                        if (   this.Polygons.Any(p => p.Intersects(contour).Item1)
                            || this.FillPolygons.Any(p => (p.Hierarchy >= contour.Hierarchy && p.IsHole && p.Shell <= nShells && p.Contains(contour))))
                        {
                            break;
                        }
                        else
                        {
                            inner_most = contour;
                            this.FillPolygons.Add(contour);
                        }
                    }
                    else
                    {
                        contour.Offset(+thickness * (double)shell, shell_miter);

                        // TODO Always add but clip with existing things?
                        //foreach (var p in this.Polygons)
                        //{
                        //    var result = p.Intersects(contour);
                        //    if (result.Item1)
                        //    {
                        //        var res = result.Item2.FirstOrDefault();
                        //        if (res != null)
                        //        {
                        //            contour = res.Union(result.Item2).First();
                        //        }
                        //    }
                        //}

                        //this.FillPolygons.Add(contour);


                        if (   this.Polygons.Any(p => p.Intersects(contour).Item1)
                            || this.FillPolygons.Any(p => p.Intersects(contour).Item1 
                                                       || (p.Hierarchy <= contour.Hierarchy && p.IsContour && p.Shell <= nShells && contour.Contains(p))))
                        {
                            break;
                        }
                        else
                        {
                            inner_most = contour;
                            this.FillPolygons.Add(contour);
                        }
                    }
                }

                // Add a dummy to clip infill, move it closer to the previous shell
                inner_most = inner_most == null ? poly.Clone() : inner_most.Clone();
                inner_most.Shell = nShells;
                inner_most.IsInfill = inner_most.IsShell = true;

                if (inner_most.IsContour)
                {
                    inner_most.Offset(-thickness / 2.0, shell_miter);

                    //if ( !(this.Polygons.Any(p => p.Intersects(inner_most).Item1
                    //                          || (p.Hierarchy >= inner_most.Hierarchy && p.IsHole && p.Contains(inner_most)))
                    //    || this.FillPolygons.Any(p => (p.Hierarchy >= inner_most.Hierarchy && p.IsHole && p.Shell < nShells && p.Contains(inner_most)))))
                    {
                        this.FillPolygons.Add(inner_most);
                    }
                }
                else
                {
                    inner_most.Offset(+thickness / 2.0, shell_miter);

                    //if ( !(this.Polygons.Any(p => p.Intersects(inner_most).Item1)
                    //    || this.FillPolygons.Any(p => p.Intersects(inner_most).Item1
                    //                               || (p.Hierarchy <= inner_most.Hierarchy && p.IsContour && p.Shell < nShells && inner_most.Contains(p)))))
                    {
                        this.FillPolygons.Add(inner_most);
                    }
                }
            }
        }

        public void AddDenseInfill(List<Polygon2D> infill_struct)
        {
            // Add this infill only to surfaces like floors and roofs

            if (this.Polygons.Any(p => p.IsSurface))
            {
                var tmp_fill = new List<Polygon2D>();
                var surfaces = this.Polygons.Where(p => p.IsSurface);

                foreach (var inf in infill_struct)
                {
                    var intersected = inf.Intersect(surfaces);
                    foreach (var p in intersected) p.CleanLines();
                    tmp_fill.AddRange(intersected);
                }

                this.FillPolygons.AddRange(Polygon2D.OrderByClosest(tmp_fill));
            }
        }

        public void AddInfill(List<Polygon2D> infill_struct)
        {
            // Add this infill to the insides

            // Intersect infill_struct with contours and subtract holes from it.
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
            foreach (var inf in infill_struct)
            {
                var intersected = inf.Intersect(inner_shell);
                foreach (var p in intersected) p.CleanLines();
                tmp_fill.AddRange(intersected);
            }

            // Sort infill on closest by
            var sorted = Polygon2D.OrderByClosest(tmp_fill);

            this.FillPolygons = other_shell.ToList();
            //tmp_fill.AddRange(inner_shell);   // Force draw infill clip polies
            //tmp_fill.AddRange(infill);        // Force draw infill
            //tmp_fill = tmp_fill[0].Union(tmp_fill).ToList();
            //this.FillPolygons.AddRange(tmp_fill[0].Union(tmp_fill));

            this.FillPolygons.AddRange(sorted);
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
