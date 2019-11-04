namespace Slicer.slyce.GCode.Commands
{
    [Command(CommandType.G, 28)]
    public class MoveToOrigin : GCodeBase
    {
        [ParameterType("X")]
        public bool? GotoX { get; set; }
        [ParameterType("Y")]
        public bool? GotoY { get; set; }
        [ParameterType("Z")]
        public bool? GotoZ { get; set; }
    }
}