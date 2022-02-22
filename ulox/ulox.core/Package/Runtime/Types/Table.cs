using System.Collections.Generic;

namespace ULox
{
    public class Table : Dictionary<HashedString, Value>
    {
        public static readonly HashedStringComparer TableKeyComparer = new HashedStringComparer();

        public Table() : base(TableKeyComparer)
        {
        }

        public IReadOnlyDictionary<HashedString, Value> AsReadOnly => this;
    }
}
