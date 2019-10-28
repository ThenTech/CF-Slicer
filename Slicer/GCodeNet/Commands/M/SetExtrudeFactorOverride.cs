namespace GCodeNet.Commands
{
    [Command(CommandType.M, 221)]
    public class SetExtrudeFactorOverride : CommandMapping
    {
        [ParameterType("S")]
        public decimal? Percentage { get; set; }
    }
}
