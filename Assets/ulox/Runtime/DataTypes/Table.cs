using System.Collections.Generic;

namespace ULox
{
    public class Table : Dictionary<string, Value>
    {
        public Table()
        {
        }

        public Table(IDictionary<string, Value> dictionary) : base(dictionary)
        {
        }

        public static Table Empty() => new Table();
    }
}
