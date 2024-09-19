using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class EnumClass : UserTypeInternal
    {
        private readonly static HashedString AllEnumHash = new("All");

        public EnumClass(HashedString name) 
            : base(name, UserType.Enum)
        {
            Fields.AddOrSet(AllEnumHash, Value.New(NativeListClass.CreateInstance()));
        }

        public EnumClass(TypeInfoEntry type) 
            : this(new HashedString(type.Name))
        {
            _typeInfoEntry = type;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddEnumValue(Value key, Value val)
        {
            var enumValue = Value.New(new EnumValue(key, val, this));
            Fields.AddOrSet(key.val.asString, enumValue);

            Fields.Get(AllEnumHash, out var found);
            (found.val.asObject as NativeListInstance).List.Add(enumValue);
        }
    }
}
