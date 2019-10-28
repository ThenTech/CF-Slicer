namespace GCodeNet.Commands
{
    [Command(CommandType.M, 220)]
    public class SetSpeedFactorOverride : CommandMapping
    {
        [ParameterType("S")]
        public decimal? Percentage { get; set; }
    }
}
