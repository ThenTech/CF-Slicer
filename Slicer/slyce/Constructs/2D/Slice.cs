using ClipperLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shape = System.Windows.Shapes.Shape;

namespace Slicer.slyce.Constructs
{
    using Path = List<IntPoint>;
    using Paths = List<List<IntPoint>>;

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
                        l.IsSurface = p.IsSurface;
                        yield return l;
                    }
                }
            }
        }

        public double MinX { get; set; }
        public double MinY { get; set; }
        public double MaxX { get; set; }
        public double MaxY { get; set; }
        public bool   HasSurface { get; set; }

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
            var eroded = new List<Polygon2D>(this.Polygons.Count);

            foreach (var poly in this.Polygons)
            {
                if (poly.IsContour)
                {
                    // Expand inwards
                    eroded.AddRange(poly.Offset(-delta, miter_limit));
                }
                else // if (poly.IsHole)
                {
                    // Expand outwards == inwards into object
                    eroded.AddRange(poly.Offset(+delta, miter_limit));
                }
            }

            this.Polygons = eroded.Where(p => p.FilterShorts()).ToList();
        }

        public void GenerateSupport(Slice above, double diameter)
        {
            if(above != null && above.Polygons != null && above.Polygons.Count > 0)
            {
                List<Polygon2D> supportPolies = new List<Polygon2D>();
                List<Polygon2D> abovePolies = new List<Polygon2D>();
                //Take union of every polygon in above (except support)
                Clipper c = new Clipper();
                foreach (var p in above.Polygons.Where(p => !p.IsSupport))
                {
                    if(p.IsComplete())
                    {
                        c.AddPath(p.IntPoints, PolyType.ptSubject, true);
                    }
                }
                PolyTree solution = new PolyTree();
                c.Execute(ClipType.ctUnion, solution);
                abovePolies.AddRange(Polygon2D.PolyNodeToPolies(solution));
               
                //Offset with -half diameter
                foreach (var p in abovePolies)
                {
                    p.Offset(-diameter / 2.0);
                }

                //Add supports from before
                c = new Clipper();
                foreach (var p in abovePolies)
                {
                    if (p.IsComplete())
                    {
                        c.AddPath(p.IntPoints, PolyType.ptSubject, true);
                    }
                }
                foreach (var p in above.Polygons.Where(p => p.IsSupport))
                {
                    if(p.IsComplete())
                    {
                        c.AddPath(p.IntPoints, PolyType.ptSubject, true);
                    }
                }
                PolyTree solution2 = new PolyTree();
                c.Execute(ClipType.ctUnion, solution2);
                abovePolies = Polygon2D.PolyNodeToPolies(solution2).ToList();

                List<Polygon2D> sliceShape = new List<Polygon2D>();
                //Union pieces in this layer
                c = new Clipper();
                foreach (var p in this.Polygons)
                {
                    if(p.IsComplete())
                    {
                        c.AddPath(p.IntPoints, PolyType.ptSubject, true);
                    }
                }
                PolyTree solution3 = new PolyTree();
                c.Execute(ClipType.ctUnion, solution3);
                sliceShape.AddRange(Polygon2D.PolyNodeToPolies(solution3));

                //Difference of that and this slice = support area
                c = new Clipper();
                foreach (var p in abovePolies)
                {
                    if (p.IsComplete())
                    {
                        c.AddPath(p.IntPoints, PolyType.ptSubject, true);
                    }
                }
                foreach (var p in sliceShape)
                {
                    if(p.IsComplete())
                    {
                        c.AddPath(p.IntPoints, PolyType.ptClip, true);
                    }
                }
                PolyTree solution4 = new PolyTree();
                c.Execute(ClipType.ctDifference, solution4);
                supportPolies.AddRange(Polygon2D.PolyNodeToPolies(solution4));

                foreach (var p in supportPolies)
                {
                    p.IsSupport = true;
                    foreach (var l in p.Lines)
                    {
                        l.IsSupport = true;
                    }
                }

                this.Polygons.AddRange(supportPolies);
            }
        }

        public void DetermineSurfaces(Slice below, Slice above)
        {
            // Subtract above/below/this.Polygons
            List<Polygon2D> thisMinusAbove = new List<Polygon2D>(); // Roofs
            List<Polygon2D> thisMinusBelow = new List<Polygon2D>(); // Floors
            if (above != null && above.Polygons != null && above.Polygons.Count != 0)
            {
                Clipper c1 = new Clipper();
                foreach (var p in this.Polygons.Where(q => !q.IsSurface))
                {
                    if (p.IsComplete())
                    {
                        c1.AddPath(p.IntPoints, PolyType.ptSubject, true);
                    }
                }

                foreach (var p in above.Polygons.Where(q => !q.IsSurface))
                {
                    if (p.IsComplete())
                    {
                        c1.AddPath(p.IntPoints, PolyType.ptClip, true);
                    }
                }

                PolyTree solution = new PolyTree();
                c1.Execute(ClipType.ctDifference, solution);

                thisMinusAbove.AddRange(Polygon2D.PolyNodeToPolies(solution));
            }
            else
            {
                thisMinusAbove.AddRange(this.Polygons.Where(p => !p.IsHole).Select(p => p.Clone()));
                //foreach (var p in this.Polygons)
                //{
                //    p.IsSurface = true;
                //    p.IsRoof = true;
                //}
            }

            if (below != null && below.Polygons != null && below.Polygons.Count() != 0)
            {
                Clipper c1 = new Clipper();
                foreach (var p in this.Polygons.Where(q => !q.IsSurface && !q.IsHole))
                {
                    if (p.IsComplete())
                    {
                        c1.AddPath(p.IntPoints, PolyType.ptSubject, true);
                    }

                }
                foreach (var p in below.Polygons.Where(q => !q.IsSurface))
                {
                    if (p.IsComplete())
                    {
                        c1.AddPath(p.IntPoints, PolyType.ptClip, true);
                    }

                }
                PolyTree solution = new PolyTree();
                c1.Execute(ClipType.ctDifference, solution);
                thisMinusBelow.AddRange(Polygon2D.PolyNodeToPolies(solution));

            }
            else
            {
                thisMinusBelow.AddRange(this.Polygons.Where(p => !p.IsHole).Select(p => p.Clone()));
                //foreach (var p in this.Polygons)
                //{
                //    p.IsSurface = true;
                //    p.IsFloor = true;
                //}
            }

            foreach (var p in thisMinusAbove)
            {
                p.IsSurface = true;
                p.IsRoof    = true;

                p.IsHole  = false;
                p.IsShell = false;
            }

            foreach (var p in thisMinusBelow)
            {
                p.IsSurface = true;
                p.IsFloor   = true;

                p.IsContour = false;
                p.IsShell   = false;
            }

            this.Polygons.AddRange(thisMinusBelow);
            this.Polygons.AddRange(thisMinusAbove);
        }

        public void AddShells(int nShells, double thickness)
        {
            // WARNING Does not take into account if shell poly intersects with other parts of the layer...
            // EDIT Now ignores shells that would overlap

            var shell_miter = 10.0;
            var polies = this.Polygons.Where(p => !p.IsSurface && !p.IsSupport).ToList();

            // Amount of overlap between infill and walls, expressed in terms of infill line width
            var infill_overlap_percentage = 0.3; // Same as Cura
            var overlapp_offset = thickness * infill_overlap_percentage;

#if false
            for (int shell = 1; shell < nShells /* -1 */; shell++)
            {
                var shells_contour = new List<Polygon2D>();
                var shells_holes = new List<Polygon2D>();
                var offset = shell < nShells
                            ? thickness * (double)shell
                            : (thickness * (double)(shell - 1)) + overlapp_offset;

                foreach (var poly in this.Polygons)
                {
                    var contour = poly.Clone();
                    contour.Shell = shell;
                    contour.IsInfill = contour.IsShell = true;

                    if (contour.IsContour)
                    {
                        shells_contour.AddRange(contour.Offset(-offset, shell_miter));
                    }
                    else
                    {
                        shells_holes.AddRange(contour.Offset(+offset, shell_miter));
                    }
                }

                var shells_contour_subtracted = new List<Polygon2D>();

                foreach (var contour in shells_contour)
                {

                    var inside = shells_holes.Where(p => contour.ContainsOrOverlaps(p)).ToList();
                    if (inside.Count > 0)
                    {
                        var diff = contour.Subtract(inside[0].Union(inside.GetRange(1, inside.Count - 1))).ToList();

                        if (diff.Count > 0)
                        {
                            shells_contour_subtracted.AddRange(diff);
                        }
                        else
                        {
                            shells_contour_subtracted.Add(contour);
                        }
                    }
                    else
                    {
                        shells_contour_subtracted.Add(contour);
                    }
                }

                Polygon2D.DetermineHierachy(ref shells_contour_subtracted);
                this.FillPolygons.AddRange(shells_contour_subtracted.Where(p => p.FilterShorts()));
            }

            var inner_shells = new List<Polygon2D>();

            foreach (var sh in this.FillPolygons)
            {
                if (sh.Shell == nShells-1)
                {
                    var inner_shell = sh.Offset((sh.IsContour ? -1.0 : 1.0) * overlapp_offset, shell_miter);
                    foreach (var inner in inner_shell)
                    {
                        inner.Shell = nShells;
                        inner.IsInfill = inner.IsShell = true;
                        inner_shells.Add(inner);
                    }
                }
            }

            this.FillPolygons.AddRange(inner_shells.Where(p => p.FilterShorts()));
#elif false
            // Note: inner most shells will be removed for infil, so add one more later.
            for (int shell = 1; shell < nShells /* -1 */; shell++)
            {
                var shells_contour = new List<Polygon2D>();
                var shells_holes = new List<Polygon2D>();
                var offset = shell < nShells
                           ? thickness * (double)shell
                           : (thickness * (double)(shell - 1)) + overlapp_offset;


                foreach (var poly in this.Polygons)
                {
                    var contour = poly.Clone();
                    contour.Shell = shell;
                    contour.IsInfill = contour.IsShell = true;

                    if (contour.IsContour)
                    {
                        shells_contour.AddRange(contour.Offset(-offset, shell_miter));
                    }
                    else
                    {
                        shells_contour.AddRange(contour.Offset(+offset, shell_miter));
                    }
                }

                var shells_contour_subtracted = new List<Polygon2D>();
                shells_contour = Polygon2D.OrderByArea(shells_contour).ToList();

                bool did_something = false;

                do
                {
                    var results = new List<Polygon2D>();
                    did_something = false;

                    for (var i = 0; i < shells_contour.Count; i++)
                    {
                        var p1 = shells_contour[i];

                        for (var j = 0; j < shells_contour.Count; j++)
                        {
                            var p2 = shells_contour[j];

                            if (i != j && !p2.WasTakenAway && p2.Hierarchy != p1.Hierarchy)
                            {
                                if (p1.ContainsOrOverlaps(p2))
                                {
                                    p1.WasTakenAway = p2.WasTakenAway = true;
                                    p1.Shell++;

                                    var p2_arr = new Polygon2D[1] { p2 };

                                    foreach (var pol in shells_contour)
                                    {
                                        if (pol.Hierarchy == p1.Hierarchy)
                                        {
                                            pol.WasTakenAway = true;
                                            if (pol.IsContour)
                                                results.AddRange(pol.Subtract(p2_arr).Select(p => { p.Shell = p1.Shell; return p; }));
                                            else
                                                results.AddRange(pol.Union(p2_arr).Select(p => { p.Shell = p1.Shell; return p; }));
                                        }
                                    }

                                    //if (p1.IsContour)
                                    //{
                                    //    results.AddRange(p1.Subtract(new Polygon2D[1] { p2 }).Select(p => { p.Shell++; return p; }));
                                    //}
                                    //else
                                    //{
                                    //    results.AddRange(p1.Union(new Polygon2D[1] { p2 }));
                                    //}

                                    results.AddRange(shells_contour.Where(p => !p.WasTakenAway));
                                    did_something = true;
                                    break;
                                }
                            }
                        }

                        if (!did_something)
                        {
                            p1.WasTakenAway = true;
                            results.Add(p1);
                        } else
                        {
                            break;
                        }
                    }

                    if (results.All(p => p.Shell > shell))
                    {
                        break;
                    }
                    else
                    {
                        shells_contour = results;
                    }
                } while (did_something);

                shells_contour.ForEach(p => p.Shell = shell);
                shells_contour_subtracted = shells_contour;

                //foreach (var contour in shells_contour)
                //{
                //    var diff = contour.Subtract(shells_holes.Where(p => /*p.Hierarchy >= contour.Hierarchy &&*/ contour.ContainsOrOverlaps(p))).ToList();

                //    if (diff.Count > 0)
                //    {
                //        shells_contour_subtracted.AddRange(diff);
                //    }
                //    else
                //    {
                //        shells_contour_subtracted.Add(contour);
                //    }
                //    break;
                //}

                if (shells_contour_subtracted.Count > 0)
                {
                    /*
                    var result = new List<Polygon2D>();

                    for (var i = 0; i < shells_contour_subtracted.Count; i++)
                    {
                        var p1 = shells_contour_subtracted[i];

                        if (p1.WasTakenAway) continue;

                        var select = new List<Polygon2D>();
                        p1.WasTakenAway = true;

                        for (var j = 0; j < shells_contour_subtracted.Count; j++)
                        {
                            var p2 = shells_contour_subtracted[j];

                            if (i != j && !p2.WasTakenAway)
                            {
                                var intersect = p1.Intersects(p2);
                                if (intersect.Item1)
                                {
                                    //select.AddRange(intersect.Item2);
                                    select.Add(p2);
                                    p2.WasTakenAway = true;
                                }
                            }
                        }

                        result.AddRange(p1.Union(select));
                        //result.AddRange(select);
                    }

                    Polygon2D.DetermineHierachy(ref result);
                    this.FillPolygons.AddRange(result.Where(p => p.FilterShorts()));
                    */

                    Polygon2D.DetermineHierachy(ref shells_contour_subtracted);
                    this.FillPolygons.AddRange(shells_contour_subtracted.Where(p => p.FilterShorts()));


                }
            }

            foreach (var poly in this.Polygons)
            {
                var inner_shell = poly.Offset((poly.IsContour ? -1.0 : 1.0) * ((thickness * (double)(nShells - 1)) + overlapp_offset), shell_miter);
                foreach (var inner in inner_shell)
                {
                    inner.Shell = nShells;
                    inner.IsInfill = inner.IsShell = true;
                    this.FillPolygons.Add(inner);
                }
            }

#else
            foreach (var poly in polies)
            {
                List<Polygon2D> inner_most = null;

                // Note: inner most shells will be removed for infil, so add one more.
                for (int shell = 1; shell < nShells /* -1 */; shell++)
                {
                    var contour = poly.Clone();
                    contour.Shell = shell;
                    contour.IsInfill = contour.IsShell = true;

                    if (poly.IsContour)
                    {
                        IEnumerable<Polygon2D> offsetted = contour.Offset(-thickness * (double)shell, shell_miter);

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

                        bool added_one = false;

                        foreach (var shp in offsetted)
                        {
                            if (polies.Any(p => p.Intersects(shp).Item1)
                                             || this.FillPolygons.Any(p => (p.Hierarchy >= shp.Hierarchy && p.IsHole && p.Shell <= nShells && p.Contains(shp))))
                            {
                                continue;
                            }
                            else
                            {
                                added_one = true;
                                this.FillPolygons.Add(shp);
                            }
                        }

                        if (added_one) inner_most = offsetted.ToList();
                    }
                    else
                    {
                        IEnumerable<Polygon2D> offsetted = contour.Offset(+thickness * (double)shell, shell_miter);

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

                        bool added_one = false;

                        foreach (var shp in offsetted)
                        {
                            if (   polies.Any(p => p.Intersects(shp).Item1)
                                || this.FillPolygons.Any(p => p.Intersects(shp).Item1
                                                           || (p.Hierarchy <= shp.Hierarchy && p.IsContour && p.Shell <= nShells && shp.Contains(p))))
                            {
                                continue;
                            }
                            else
                            {
                                added_one = true;
                                this.FillPolygons.Add(shp);
                            }
                        }

                        if (added_one) inner_most = offsetted.ToList();
                    }
                }

                if (inner_most == null || inner_most.Count == 0)
                {
                    inner_most = new List<Polygon2D>() { poly.Clone() };
                }

                // Add a dummy to clip infill, move it closer to the previous shell
                foreach (var inner in inner_most)
                {
                    inner.Shell = nShells;
                    inner.IsInfill = inner.IsShell = true;

                    if (inner.IsContour)
                    {
                        var offsetted = inner.Offset(-overlapp_offset, shell_miter);

                        foreach (var off in offsetted)
                        {
                            //if ( !(this.Polygons.Any(p => p.Intersects(off).Item1
                            //                           || (p.Hierarchy >= off.Hierarchy && p.IsHole && p.Contains(off)))
                            //    || this.FillPolygons.Any(p => (p.Hierarchy >= off.Hierarchy && p.IsHole && p.Shell < nShells && p.Contains(off)))))
                            {
                                this.FillPolygons.Add(off);
                            }
                        }
                    }
                    else
                    {
                        var offsetted = inner.Offset(+overlapp_offset, shell_miter);

                        foreach (var off in offsetted)
                        {
                            //if ( !(this.Polygons.Any(p => p.Intersects(off).Item1)
                            //    || this.FillPolygons.Any(p => p.Intersects(off).Item1
                            //                               || (p.Hierarchy <= off.Hierarchy && p.IsContour && p.Shell < nShells && off.Contains(p)))))
                            {
                                this.FillPolygons.Add(off);
                            }
                        }
                    }
                }
            }

            this.FillPolygons = this.FillPolygons.Where(p => p.FilterShorts()).ToList();
#endif
        }

        public void AddDenseInfill(List<Polygon2D> infill_struct)
        {
            // Add this infill only to surfaces like floors and roofs

            if (this.Polygons.Any(p => p.IsSurface))
            {
                var tmp_fill = new List<Polygon2D>();
                var surfaces = this.Polygons.Where(p => p.IsSurface).ToList();

                Clipper c = new Clipper();

                foreach (var inf in infill_struct)
                {
                    var intersected = inf.Intersect(surfaces);
                    foreach (var p in intersected) {
                        p.CleanLines();
                        c.AddPath(p.IntPoints, PolyType.ptSubject, false);
                    }
                }

                PolyTree solution = new PolyTree();
                c.Execute(ClipType.ctUnion, solution);

                foreach (var p in Polygon2D.PolyNodeToPolies(solution))
                {
                    p.IsSurface = true;
                    tmp_fill.Add(p);
                }

                this.FillPolygons.AddRange(Polygon2D.OrderByClosest(tmp_fill));
            }
        }

        public void AddInfill(List<Polygon2D> infill_struct)
        {
            // Add this infill to the insides

            //var nShells = 3;
            //var thickness = 0.4 * 0.95;

            //var shell_miter = 10.0;
            //var infill_overlap_percentage = 0.3; // Same as Cura
            //var overlapp_offset = thickness * infill_overlap_percentage;


            //var tmp_fill = new List<Polygon2D>();

            //foreach (var inf in infill_struct)
            //{
            //    var intersected = inf.Intersect(this.Polygons);
            //    foreach (var p in intersected)
            //    {
            //        p.IsInfill = true;
            //        p.CleanLines();
            //        tmp_fill.AddRange(p.Offset(-(double)(nShells - 1) * thickness + overlapp_offset, shell_miter)
            //                           .Where(of => of.FilterShorts()));
            //    }
            //}

            //this.FillPolygons.AddRange(Polygon2D.OrderByClosest(tmp_fill));





            // Intersect infill_struct with contours and subtract holes from it.
            var tmp_dense_fill = new List<Polygon2D>();
            var tmp_fill = new List<Polygon2D>();

            IEnumerable<Polygon2D> inner_shell = null;
            IEnumerable<Polygon2D> other_shell = null;
            IEnumerable<Polygon2D> dense_fill  = null;

            var surfaces = this.Polygons.Where(p => p.IsSurface).ToList();

            if (this.FillPolygons.Count > 0)
            {
                // Has shell
                var most_inner_shell = this.FillPolygons.Max(p => p.Shell);
                inner_shell = this.FillPolygons.Where(p => !p.IsSurface && p.Shell == most_inner_shell).ToList();
                other_shell = this.FillPolygons.Where(p => !p.IsSurface && p.Shell < most_inner_shell).ToList();
                dense_fill  = this.FillPolygons.Where(p => p.IsSurface).ToList();
            }
            else
            {
                inner_shell = this.Polygons.Where(p => !p.IsSurface);
                other_shell = new List<Polygon2D>();
                dense_fill  = new List<Polygon2D>();
            }

            // Intersect each surface fill with inner shell
            foreach (var inf in this.FillPolygons.Where(p => p.IsSurface))
            {
                var intersected = inf.Intersect(inner_shell);
                foreach (var p in intersected)
                {
                    p.IsSurface = true;
                    p.IsInfill  = true;
                    p.CleanLines();
                }
                tmp_dense_fill.AddRange(intersected.Where(p => p.FilterShorts()));
            }

            // Intersect each poly from infill with each one of inner shells
            // And subtract the Surface polies, since these already have dense infil
            foreach (var inf in infill_struct)
            {
                var intersected = inf.Intersect(inner_shell);

                foreach (var p in intersected)
                {
                    var result = p.Subtract(surfaces);
                    foreach (var q in result)
                    {
                        q.IsInfill = true;
                        q.CleanLines();
                    }
                    tmp_fill.AddRange(result.Where(q => q.FilterShorts()));
                }
            }

            // Add shells from inside to outside
            this.FillPolygons = other_shell.ToList();  // .Reverse()

            // Sort infill on closest by
            tmp_dense_fill.AddRange(tmp_fill);
            var sorted = Polygon2D.OrderByClosest(tmp_dense_fill);

            //tmp_fill.AddRange(inner_shell);   // Force draw infill clip polies
            //tmp_fill.AddRange(infill);        // Force draw infill
            //tmp_fill = tmp_fill[0].Union(tmp_fill).ToList();
            //this.FillPolygons.AddRange(tmp_fill[0].Union(tmp_fill));

            this.FillPolygons.AddRange(sorted);

            // Remove surfaces as they are now handled
            this.Polygons = this.Polygons.Where(p => !p.IsSurface).ToList();
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

        public void SortPolygons(bool outer_to_inner = false)
        {
            this.Polygons = Polygon2D.OrderByHierarchy(this.Polygons, outer_to_inner).ToList();
        }
    }
}
