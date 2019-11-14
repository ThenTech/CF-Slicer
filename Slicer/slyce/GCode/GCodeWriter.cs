using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClipperLib;
using Path = System.Collections.Generic.List<ClipperLib.IntPoint>;
using Paths = System.Collections.Generic.List<System.Collections.Generic.List<ClipperLib.IntPoint>>;


using Slicer.slyce.GCode.Commands;
using Slicer.slyce.Constructs._2D;


namespace Slicer.slyce.GCode
{
    public class CommandCollection : List<GCodeBase>
    { }

    class GCodeWriter
    {
        private CommandCollection instructions;

        private static readonly string header_comment   = ";Generated with Slycer - Lieven Libberecht, William Thenaers";
        private static readonly string startup_comment  = "\n;Setup";
        private static readonly string print_comment    = "\n;Print";
        private static readonly string teardown_comment = "\n;Teardown";

        private static readonly CommandCollection startup_sequence = new CommandCollection()
        {
            new SetBedTemperature() { Temperature = 200 },
            new SetBedTemperatureAndWait() { MinTemperature = 200 },
            new SetExtruderTemperature() { Temperature = 200 },
            new SetExtruderTemperatureAndWait() { MinTemperature = 200 },

            new SetExtruderToAbsolute(),
            new SetUnitsToMillimeters(),
            new MoveToOrigin(),

            new SetPosition() { Extrude = 0 },
            new LinearMove() { MoveZ = 2.0m, Feedrate=3000},
            new LinearMove() { MoveX = 0.1m, MoveY =  20.0m, MoveZ = 0.3m, Feedrate=5000 },
            new LinearMove() { MoveX = 0.1m, MoveY = 200.0m, MoveZ = 0.3m, Feedrate=1500, Extrude = 15},
            new LinearMove() { MoveX = 0.4m, MoveY = 200.0m, MoveZ = 0.3m, Feedrate=5000 },
            new LinearMove() { MoveX = 0.4m, MoveY =  20.0m, MoveZ = 0.3m, Feedrate=1500, Extrude = 30 },

            new SetPosition() { Extrude = 0 },
            new LinearMove() { MoveZ = 2.0m, Feedrate=3000},

            new SetPosition() { Extrude = 0 },
            new LinearMove() { Feedrate=2400, Extrude = -5 },

            new FanOff(),
            new LinearMove() { Feedrate=1500, Extrude = 0 },
        };

        private static readonly CommandCollection teardown_sequence = new CommandCollection()
        {
            new SetBedTemperature() { Temperature = 0 },
            new FanOff(),
            new SetSpeedFactorOverride() { Percentage = 100 },
            new SetExtrudeFactorOverride() { Percentage = 100 },

            new SetRelativePositioning(),
            new LinearMove() { Feedrate=1800, Extrude = -3 },
            new LinearMove() { MoveZ = 20, Feedrate=3000 },
            
            new SetAbsolutePositioning(),
            new LinearMove() { MoveX = 0, MoveY = 235, Feedrate=1000 },

            new FanOff(),
            new StopIdleHold(),
            new SetExtruderToAbsolute(),
            new SetExtruderTemperature() { Temperature = 0 },
        };

        public GCodeWriter()
        {
            this.instructions = new CommandCollection();
        }

        public void Reset()
        {
            this.instructions.Clear();
        }

        public void Setup()
        {
            this.instructions.AddRange(GCodeWriter.startup_sequence);
        }

        public void Add(GCodeBase cmd)
        {
            this.instructions.Add(cmd);
        }

        public void AddSlice(Slice s, double layer_height, double nozzle_diam, double filament_diam)
        {
            // TODO add FanOn() after first layer
            // TODO set s.Z position as current layer Z position

            // foreach (var p in s.Polygons)
            {
                int count = s.Lines.Count();

                if (count > 1)
                {
                    var current_point = s.Lines[0].StartPoint;

                    var move_start = new SetPosition()
                    {
                        MoveX = (decimal)current_point.X,
                        MoveY = (decimal)current_point.Y,
                        MoveZ = (decimal)s.Z,
                        Extrude = 0
                    };

                    this.Add(move_start);

                    for (int i = 1; i < count; i++)
                    {
                        var next_point = s.Lines[i].StartPoint;

                        double extrusion_length = (layer_height * nozzle_diam * s.Lines[i - 1].GetLength()) / filament_diam;

                        var move_next = new LinearMove()
                        {
                            MoveX = (decimal)next_point.X,
                            MoveY = (decimal)next_point.Y,
                            Extrude = (decimal)extrusion_length
                        };
                        this.Add(move_next);
                    }

                    // Move nozzle back up a little
                    this.Add(new SetPosition() { Extrude = 0 });
                    this.Add(new LinearMove()  { MoveZ = (decimal)(s.Z + layer_height), Feedrate = 3000 });
                    this.Add(new SetPosition() { Extrude = 0 });
                    this.Add(new LinearMove()  { Feedrate = 2400, Extrude = -5 });
                }
            }

            /*
            // foreach (var p in s.FillPolygons)
            {
                // Generate filling for surface of these polies          
                var obj         = new Paths();  // Current FillPoly to fill with infill
                var filler_grid = new Paths();  // Infill grid structure

                Paths solution = new Paths();
                Clipper c = new Clipper();
                c.AddPolygons(obj, PolyType.ptSubject);
                c.AddPolygons(filler_grid, PolyType.ptClip);

                // Intersect fill poly with infill
                c.Execute(ClipType.ctIntersection, solution);

                // Add solution to gcode?
            }
            */

            // Move nozzle back up a little
            this.Add(new SetPosition() { Extrude = 0 });
            this.Add(new LinearMove()  { MoveZ = (decimal)(s.Z + layer_height), Feedrate = 3000 });
            this.Add(new SetPosition() { Extrude = 0 });
            this.Add(new LinearMove()  { Feedrate = 2400, Extrude = -5 });
        }

        public void ExportToFile(Uri path, bool insert_setup = true, bool append_teardown = true)
        {
            this.ExportToFile(path.ToString(), insert_setup, append_teardown);
        }

        public void ExportToFile(string path, bool insert_setup=true, bool append_teardown=true)
        {
            var fout = new StreamWriter(path);

            fout.WriteLine(GCodeWriter.header_comment);
            
            if (insert_setup)
            {
                fout.WriteLine(GCodeWriter.startup_comment);
                GCodeWriter.startup_sequence.ForEach(x => {
                    fout.WriteLine(x.ToString());
                });
            }

            fout.WriteLine(GCodeWriter.print_comment);
            this.instructions.ForEach(x => {
                fout.WriteLine(x.ToString());
            });

            if (append_teardown)
            {
                fout.WriteLine(GCodeWriter.teardown_comment);
                GCodeWriter.teardown_sequence.ForEach(x => {
                    fout.WriteLine(x.ToString());
                });
            }

            fout.Flush();
            fout.Close();
        }
    }
}
