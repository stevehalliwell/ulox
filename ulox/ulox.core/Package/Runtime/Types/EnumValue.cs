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
            Fields[KeyHash] = key;
            Fields[ValueHash] = val;
            Fields[FromEnumHash] = Value.New(from);
            ReadOnly();
        }

        public override string ToString()
        {
            return $"<{nameof(EnumValue)} {FromUserType.Name}.{Fields[KeyHash]} ({Fields[ValueHash]})>";
        }
    }
}
