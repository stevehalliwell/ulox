using System.Collections.Generic;

namespace ULox
{
    public class Table : Dictionary<HashedString, Value>
    {
        public readonly static HashedStringComparer TableKeyComparer = new HashedStringComparer();
        public Table():base(TableKeyComparer)
        {
        }
    }
}
