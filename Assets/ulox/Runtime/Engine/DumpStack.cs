using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ULox
{
    public class DumpStack
    {
        private readonly StringBuilder sb = new StringBuilder();

        public string Generate(IEnumerable<Value> enumerableValueStack)
        {
            var valueStack = enumerableValueStack.ToList();

            for (int i = valueStack.Count - 1; i >= 0; i--)
            {
                sb.Append(valueStack[i].ToString());
                if (i > 0)
                    sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
