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

        public ClassInternal FromClass { get; protected set; }
        public bool IsFrozen { get; private set; } = false;
        public Table Fields { get; protected set; } = new Table();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasField(HashedString key) => Fields.ContainsKey(key);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetField(HashedString key, Value val)
        {
            if (!IsFrozen || Fields.ContainsKey(key))
                Fields[key] = val;
            else
                throw new FreezeException($"Attempted to Create a new field '{key}' via SetField on a frozen object. This is not allowed.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Value GetField(HashedString key)
        {
            if (Fields.TryGetValue(key, out var ret))
                return ret;

            throw new System.Exception();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetField(HashedString key, out Value val) => Fields.TryGetValue(key, out val);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RemoveField(HashedString fieldNameStr) => Fields.Remove(fieldNameStr);

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFrom(InstanceInternal inst)
        {
            FromClass = inst.FromClass;
            foreach (var keyPair in inst.Fields)
            {
                Fields[keyPair.Key] = keyPair.Value;
            }
            IsFrozen = inst.IsFrozen;
        }
    }
}
