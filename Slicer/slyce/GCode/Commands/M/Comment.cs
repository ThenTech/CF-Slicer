using System.Linq;
using System.Text;

namespace Slicer.slyce.GCode.Commands
{
    [Command(CommandType.M, -1)]
    public class Comment : GCodeBase
    {
        [ParameterType("C")]
        public string Text { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if (!string.IsNullOrEmpty(Text))
            {
                var linefeed = Text.StartsWith("\n") ? "\n" : "";
                sb.Append(linefeed + ";" + Text.TrimStart());
            }

            return sb.ToString();
        }
    }
}
