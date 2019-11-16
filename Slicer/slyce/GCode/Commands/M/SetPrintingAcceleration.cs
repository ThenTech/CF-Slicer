namespace Slicer.slyce.GCode.Commands
{
    [Command(CommandType.M, 201)]
    public class SetPrintingAcceleration : GCodeBase
    {
        [ParameterType("X")]
        public decimal? AccX { get; set; }
        [ParameterType("Y")]
        public decimal? AccY { get; set; }
        [ParameterType("Z")]
        public decimal? AccZ { get; set; }
        [ParameterType("E")]
        public decimal? AccE { get; set; }
    }
}
