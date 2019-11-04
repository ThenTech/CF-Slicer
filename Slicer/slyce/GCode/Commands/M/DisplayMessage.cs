using System.Linq;
using System.Text;

namespace Slicer.slyce.GCode.Commands
{
    [Command(CommandType.M, 117)]
    public class DisplayMessage : GCodeBase
    {
        [ParameterType("D")]
        public string Message { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            
            if (!string.IsNullOrEmpty(Message))
            {
                var cmdattrib = this.GetType().GetCustomAttributes(typeof(CommandAttribute), true).FirstOrDefault() as CommandAttribute;
                sb.Append(cmdattrib.CommandType);
                sb.Append(cmdattrib.CommandSubType);
                sb.Append(" " + Message);
            }

            return sb.ToString();
        }
    }
}
