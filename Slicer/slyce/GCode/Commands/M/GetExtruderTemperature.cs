namespace Slicer.slyce.GCode.Commands
{
    [Command(CommandType.M, 105)]
    public class GetExtruderTemperature : GCodeBase
    {
        [ParameterType("R")]
        public int? ResponseSequence { get; set; }
        [ParameterType("S")]
        public int? ResponseType { get; set; }
    }
}
