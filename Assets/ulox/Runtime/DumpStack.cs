using System;
using System.Collections.Generic;
using System.Text;

namespace ULox
{
    public class DumpStack
    {
        private StringBuilder sb = new StringBuilder();

        public string Generate(FastStack<Value> valueStack)
        {
            for (int i = valueStack.Count - 1; i >= 0; i--)
            {
                sb.Append(valueStack[i].ToString());
                if (i > 0)
                    sb.AppendLine();
            }

            return sb.ToString();
        }
    }

    public class DumpGlobals
    {
        private StringBuilder sb = new StringBuilder();

        public string Generate(IndexedTable globals)
        {
            int count = 0;
            foreach (var item in globals)
            {
                sb.Append($"{item.Key} : {item.Value}");

                if (count != globals.Count)
                    sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
