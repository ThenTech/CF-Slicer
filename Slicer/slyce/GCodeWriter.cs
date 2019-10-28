using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GCodeNet;
using GCodeNet.Commands;

namespace Slicer.slyce
{
    class GCodeWriter
    {
        private CommandCollection instructions;

        private static readonly CommandCollection startup_sequence = new CommandCollection()
        {
            new SetBedTemperature() { Temperature = 200 },
            new SetBedTemperatureAndWait() { MinTemperature = 200 },
            new SetExtruderTemperature() { Temperature = 200 },
            new SetExtruderTemperatureAndWait() { MinTemperature = 200 },

            new SetExtruderToAbsolute(),
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

        }

        public void Reset()
        {
            this.instructions.Clear();
        }

        public void Setup()
        {
            this.instructions.AddRange(GCodeWriter.startup_sequence);
        }

        public void Add(CommandBase cmd)
        {
            this.instructions.Add(cmd);
        }

        public void ExportToFile(Uri path, bool insert_setup=true, bool append_teardown=true)
        {
            var fout = new StreamWriter(path.ToString());

            if (insert_setup)
            {
                GCodeWriter.startup_sequence.ForEach(x => {
                    fout.Write(x.ToGCode());
                });
            }

            this.instructions.ForEach(x => {
                fout.Write(x.ToGCode());
            });

            if (append_teardown)
            {
                GCodeWriter.teardown_sequence.ForEach(x => {
                    fout.Write(x.ToGCode());
                });
            }

            fout.Flush();
            fout.Close();
        }
    }
}
