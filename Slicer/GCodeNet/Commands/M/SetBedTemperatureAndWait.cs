namespace GCodeNet.Commands
{
    [Command(CommandType.M, 190)]
    public class SetBedTemperatureAndWait : CommandMapping
    {
        [ParameterType("S")]
        public int? MinTemperature { get; set; }
        [ParameterType("R")]
        public int? AccurateTargetTemperature { get; set; }
    }
}