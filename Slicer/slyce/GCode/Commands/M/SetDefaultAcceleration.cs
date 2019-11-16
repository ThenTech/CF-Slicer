namespace Slicer.slyce.GCode.Commands
{
    [Command(CommandType.M, 204)]
    public class SetDefaultAcceleration : GCodeBase
    {
        [ParameterType("P")]
        public decimal? Printing { get; set; }

        [ParameterType("R")]
        public decimal? Retracting { get; set; }

        [ParameterType("T")]
        public decimal? Travel { get; set; }
    }
}
