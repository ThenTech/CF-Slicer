namespace Slicer.slyce.GCode.Commands
{
    [Command(CommandType.M, 190)]
    public class SetBedTemperatureAndWait : GCodeBase
    {
        [ParameterType("S")]
        public int? MinTemperature { get; set; }
        [ParameterType("R")]
        public int? AccurateTargetTemperature { get; set; }
    }
}