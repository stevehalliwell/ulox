namespace ULox
{
    public class InstanceInternal
    {
        public InstanceInternal()
        {
        }

        public InstanceInternal(ClassInternal fromClass)
        {
            FromClass = fromClass;
        }

        public InstanceInternal(
            ClassInternal fromClass,
            Table fields)
        {
            FromClass = fromClass;
            Fields = fields;
        }

        public InstanceInternal(
            ClassInternal fromClass,
            Table fields,
            bool freeze)
        {
            FromClass = fromClass;
            Fields = fields;
            IsFrozen = freeze;
        }

        public ClassInternal FromClass { get; protected set; }
        public bool IsFrozen { get; private set; } = false;
        public Table Fields { get; protected set; } = Table.Empty();

        public bool HasField(string key) => Fields.ContainsKey(key);

        public void SetField(string key, Value val)
        {
            if (!IsFrozen || Fields.ContainsKey(key))
                Fields[key] = val;
            else
                throw new FreezeException($"Attempted to Create a new field '{key}' via SetField on a frozen object. This is not allowed.");
        }

        public Value GetField(string key)
        {
            if (Fields.TryGetValue(key, out var ret))
                return ret;

            throw new System.Exception();
        }

        public bool TryGetField(string key, out Value val) => Fields.TryGetValue(key, out val);

        public bool RemoveField(string fieldNameStr) => Fields.Remove(fieldNameStr);

        public void Freeze()
        {
            IsFrozen = true;
        }
    }
}
