namespace Slicer.slyce.GCode.Commands
{
    [Command(CommandType.M, 140)]
    public class SetBedTemperature : GCodeBase
    {
        [ParameterType("S")]
        public int? Temperature { get; set; }
    }
}