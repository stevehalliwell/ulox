using System;
using System.Collections.Generic;

namespace ULox
{
    public class Table : Dictionary<string, Value>
    {
        public static Table Empty()
        {
            return new Table();
        }
    }
}
