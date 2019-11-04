namespace Slicer.slyce.GCode.Commands
{
    [Command(CommandType.M, 104)]
    public class SetExtruderTemperature : GCodeBase
    {
        [ParameterType("S")]
        public int? Temperature { get; set; }
    }
}