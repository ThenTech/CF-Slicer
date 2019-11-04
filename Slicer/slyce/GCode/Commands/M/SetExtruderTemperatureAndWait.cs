namespace Slicer.slyce.GCode.Commands
{
    [Command(CommandType.M, 109)]
    public class SetExtruderTemperatureAndWait : GCodeBase
    {
        [ParameterType("S")]
        public int? MinTemperature { get; set; }
        [ParameterType("R")]
        public int? AccurateTargetTemperature { get; set; }
    }
}