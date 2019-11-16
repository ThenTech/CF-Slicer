namespace Slicer.slyce.GCode.Commands
{
    [Command(CommandType.M, 205)]
    public class SetAdvancedSettings : GCodeBase
    {
        [ParameterType("S")]
        public decimal? Printing { get; set; }
        [ParameterType("T")]
        public decimal? Travel { get; set; }
        [ParameterType("B")]
        public decimal? MinSegmentTime { get; set; }
        [ParameterType("X")]
        public decimal? MaxXJerk { get; set; }
        [ParameterType("Y")]
        public decimal? MaxYJerk { get; set; }
        [ParameterType("Z")]
        public decimal? MaxZJerk { get; set; }
        [ParameterType("E")]
        public decimal? MaxEJerk { get; set; }
    }
}
