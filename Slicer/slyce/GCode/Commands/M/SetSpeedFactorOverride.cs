namespace Slicer.slyce.GCode.Commands
{
    [Command(CommandType.M, 220)]
    public class SetSpeedFactorOverride : GCodeBase
    {
        [ParameterType("S")]
        public decimal? Percentage { get; set; }
    }
}
