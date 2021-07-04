﻿using System.Collections.Generic;
using System.Text;

namespace ULox
{
    public class DumpGlobals
    {
        private readonly StringBuilder sb = new StringBuilder();

        public string Generate(IEnumerable<KeyValuePair<string, Value>> globals)
        {
            foreach (var item in globals)
            {
                sb.Append($"{item.Key} : {item.Value}");
            }

            return sb.ToString();
        }
    }
}
