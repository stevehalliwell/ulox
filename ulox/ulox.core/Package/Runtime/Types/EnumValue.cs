namespace ULox
{
    public sealed class EnumValue : InstanceInternal
    {
        public readonly static HashedString KeyHash = new("Key");
        public readonly static HashedString ValueHash = new("Value");
        public readonly static HashedString FromEnumHash = new("Enum");

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
            Fields.Get(KeyHash, out var keyVal);
            Fields.Get(ValueHash, out var valVal);
            return $"<{nameof(EnumValue)} {FromUserType.Name}.{keyVal} ({valVal})>";
        }
    }
}
