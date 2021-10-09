using System.Runtime.CompilerServices;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasField(string key) => Fields.ContainsKey(key);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetField(string key, Value val)
        {
            if (!IsFrozen || Fields.ContainsKey(key))
                Fields[key] = val;
            else
                throw new FreezeException($"Attempted to Create a new field '{key}' via SetField on a frozen object. This is not allowed.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Value GetField(string key)
        {
            if (Fields.TryGetValue(key, out var ret))
                return ret;

            throw new System.Exception();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetField(string key, out Value val) => Fields.TryGetValue(key, out val);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RemoveField(string fieldNameStr) => Fields.Remove(fieldNameStr);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Freeze()
        {
            IsFrozen = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unfreeze()
        {
            IsFrozen = false;
        }
    }
}
