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
            if (from != null && from.FieldNames.Count > 0)
                _fieldSlots = new Value[from.FieldNames.Count];
        }

        public Table Fields { get; protected set; } = new Table();
        private Value[] _fieldSlots;
        public UserTypeInternal FromUserType { get; protected set; }
        public bool IsFrozen { get; private set; } = false;
        public bool IsReadOnly { get; protected set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetField(HashedString key, Value val)
        {
            if (IsReadOnly)
                throw new UloxException($"Attempted to Set field '{key}', but instance is read only.");

            if (IsFrozen)
                Fields.Set(key, val);
            else
                Fields.AddOrSet(key,val);

            if (_fieldSlots != null && FromUserType.TryGetFieldSlot(key, out var slot))
                _fieldSlots[slot] = val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetExistingField(HashedString key, Value val)
        {
            if (IsReadOnly)
                throw new UloxException($"Attempted to Set field '{key}', but instance is read only.");

            Fields.Set(key, val);

            if (_fieldSlots != null && FromUserType.TryGetFieldSlot(key, out var slot))
                _fieldSlots[slot] = val;
        }

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
            Fields.CopyFrom(inst.Fields);
            if (inst._fieldSlots != null)
                _fieldSlots = (Value[])inst._fieldSlots.Clone();
            IsFrozen = inst.IsFrozen;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetFixedField(HashedString key, out Value val)
        {
            if (_fieldSlots != null && FromUserType.TryGetFieldSlot(key, out var slot))
            {
                val = _fieldSlots[slot];
                return true;
            }

            val = default;
            return false;
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
