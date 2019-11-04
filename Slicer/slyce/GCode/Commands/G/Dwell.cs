namespace Slicer.slyce.GCode.Commands
{
    [Command(CommandType.G, 4)]
    public class Dwell : GCodeBase
    {
        [ParameterType("P")]
        public decimal? WaitInMSecs { get; set; }
        [ParameterType("S")]
        public decimal? WaitInSecs { get; set; }
    }
}