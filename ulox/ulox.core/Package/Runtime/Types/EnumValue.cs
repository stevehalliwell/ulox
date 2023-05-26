namespace ULox
{
    public sealed class EnumValue : InstanceInternal
    {
        public readonly static HashedString KeyHash = new HashedString("Key");
        public readonly static HashedString ValueHash = new HashedString("Value");
        public readonly static HashedString FromEnumHash = new HashedString("Enum");

        public EnumValue(Value key, Value val, UserTypeInternal from)
            : base(from)
        {
            Fields.AddOrSet(KeyHash, key);
            Fields.AddOrSet(ValueHash, val);
            Fields.AddOrSet(FromEnumHash, Value.New(from));
            ReadOnly();
        }

        public override string ToString()
        {
            Fields.Get(KeyHash.Hash, out var keyVal);
            Fields.Get(ValueHash.Hash, out var valVal);
            return $"<{nameof(EnumValue)} {FromUserType.Name}.{keyVal} ({valVal})>";
        }
    }
}
