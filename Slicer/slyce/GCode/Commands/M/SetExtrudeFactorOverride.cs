namespace Slicer.slyce.GCode.Commands
{
    [Command(CommandType.M, 221)]
    public class SetExtrudeFactorOverride : GCodeBase
    {
        [ParameterType("S")]
        public decimal? Percentage { get; set; }
    }
}
