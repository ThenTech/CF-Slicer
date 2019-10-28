namespace GCodeNet.Commands
{
    [Command(CommandType.M, 140)]
    public class SetBedTemperature : CommandMapping
    {
        [ParameterType("S")]
        public int? Temperature { get; set; }
    }
}