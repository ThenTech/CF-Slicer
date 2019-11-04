using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Slicer.slyce.GCode
{
    public enum CommandType
    {
        G, M
    }

    public enum ParameterType
    {
        T, S, P, X, Y, Z, I, J, D, H, F, R, Q, E
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class CommandAttribute : Attribute
    {
        public CommandType CommandType { get; private set; }
        public int CommandSubType { get; private set; }

        public CommandAttribute(CommandType commandType, int commandSubType)
        {
            this.CommandType = commandType;
            this.CommandSubType = commandSubType;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ParameterTypeAttribute : Attribute
    {
        public ParameterType Param { get; private set; }
        public ParameterTypeAttribute(ParameterType param)
        {
            this.Param = param;
        }

        public ParameterTypeAttribute(string param)
        {
            this.Param = (ParameterType)Enum.Parse(typeof(ParameterType), param);
        }
    }

    public abstract class GCodeBase
    {
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            var cmdattrib = this.GetType().GetCustomAttributes(typeof(CommandAttribute), true).FirstOrDefault() as CommandAttribute;
            if (cmdattrib == null)
            {
                throw new Exception("Mo CommandAttribute attribute");
            }

            sb.Append(cmdattrib.CommandType);
            sb.Append(cmdattrib.CommandSubType);

            foreach (var prop in this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var attrib = (ParameterTypeAttribute)prop.GetCustomAttributes(typeof(ParameterTypeAttribute), true).SingleOrDefault();
                if (attrib != null)
                {
                    var key = attrib.Param;
                    var val = prop.GetValue(this, null);

                    if (prop.PropertyType.Equals(typeof(bool)) && (bool)val == true)
                    {
                        continue;
                    }
                    else if (val != null)
                    {
                        sb.Append(" " + key);
                        if (prop.PropertyType.IsEnum)
                        {
                            sb.Append((int)val);
                        } else
                        {
                            sb.Append(val);
                        }
                    }
                }
            }

            return sb.ToString();
        }
    }
}
