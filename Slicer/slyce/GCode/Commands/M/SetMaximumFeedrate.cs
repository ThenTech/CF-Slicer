namespace Slicer.slyce.GCode.Commands
{
    [Command(CommandType.M, 203)]
    public class SetMaximumFeedrate : GCodeBase
    {
        [ParameterType("X")]
        public decimal? FeedX { get; set; }
        [ParameterType("Y")]
        public decimal? FeedY { get; set; }
        [ParameterType("Z")]
        public decimal? FeedZ { get; set; }
        [ParameterType("E")]
        public decimal? FeedE { get; set; }
    }
}
