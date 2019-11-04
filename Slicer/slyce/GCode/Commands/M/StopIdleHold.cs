namespace Slicer.slyce.GCode.Commands
{
    [Command(CommandType.M, 84)]
    public class StopIdleHold : GCodeBase
    {
        [ParameterType("I")]
        public int? ResetFlags { get; set; }
    }
}