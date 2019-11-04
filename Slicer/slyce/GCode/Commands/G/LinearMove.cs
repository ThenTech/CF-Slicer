namespace Slicer.slyce.GCode.Commands
{
    [Command(CommandType.G, 1)]
    public class LinearMove : GCodeBase
    {
        [ParameterType("X")]
        public decimal? MoveX { get; set; }
        [ParameterType("Y")]
        public decimal? MoveY { get; set; }
        [ParameterType("Z")]
        public decimal? MoveZ { get; set; }
        [ParameterType("E")]
        public decimal? Extrude { get; set; }
        [ParameterType("F")]
        public decimal? Feedrate { get; set; }
        [ParameterType("S")]
        public CheckEndstop? CheckEndstop { get; set; }
    }

    public enum CheckEndstop
    {
        Ignore = 0,
        Check = 1,
    }
}
