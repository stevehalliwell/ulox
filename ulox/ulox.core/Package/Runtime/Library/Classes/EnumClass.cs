using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class EnumClass : UserTypeInternal
    {
        private readonly static HashedString AllEnumHash = new HashedString("All");

        public EnumClass(HashedString name) 
            : base(name, UserType.Enum)
        {
            Fields.AddOrSet(AllEnumHash, Value.New(NativeListClass.CreateInstance()));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddEnumValue(Value key, Value val)
        {
            var enumValue = Value.New(new EnumValue(key, val, this));
            Fields.AddOrSet(key.val.asString, enumValue);

            Fields.Get(AllEnumHash.Hash, out var found);
            (found.val.asObject as NativeListInstance).List.Add(enumValue);
        }
    }
}
