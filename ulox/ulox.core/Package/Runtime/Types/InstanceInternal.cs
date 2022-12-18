using System.Runtime.CompilerServices;

namespace ULox
{
    public class InstanceInternal
    {
        public InstanceInternal()
            : this(null)
        {
        }

        public InstanceInternal(UserTypeInternal from)
        {
            FromUserType = from;
        }

        public UserTypeInternal FromUserType { get; protected set; }
        public bool IsFrozen { get; private set; } = false;
        public Table Fields { get; protected set; } = new Table();
        public bool IsReadOnly { get; protected set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasField(HashedString key) => Fields.ContainsKey(key);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetField(HashedString key, Value val)
        {
            if (IsReadOnly)
                throw new UloxException($"Attempted to Set field '{key}', but instance is read only.");

            if (!IsFrozen
                || Fields.ContainsKey(key))
                Fields[key] = val;
            else
                throw new UloxException($"Attempted to Create a new field '{key}' via SetField on a frozen object.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Value GetField(HashedString key)
        {
            if (Fields.TryGetValue(key, out var ret))
                return ret;

            throw new UloxException($"Attempted to Get a new field '{key}', but none exists.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetField(HashedString key, out Value val)
            => Fields.TryGetValue(key, out val);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RemoveField(HashedString fieldNameStr)
            => Fields.Remove(fieldNameStr);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Freeze()
            => IsFrozen = true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unfreeze()
            => IsFrozen = false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFrom(InstanceInternal inst)
        {
            FromUserType = inst.FromUserType;
            foreach (var keyPair in inst.Fields)
            {
                Fields[keyPair.Key] = keyPair.Value;
            }
            IsFrozen = inst.IsFrozen;
        }

        public override string ToString() => $"<inst {FromUserType?.Name}>";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadOnly()
        {
            if (IsReadOnly) return;

            IsReadOnly = true;

            foreach (var field in Fields)
            {
                if (field.Value.val.asObject is InstanceInternal inst)
                {
                    inst.ReadOnly();
                }
            }
        }
    }
}
