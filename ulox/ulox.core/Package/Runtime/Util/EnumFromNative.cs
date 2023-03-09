using System;
using System.Linq;

namespace ULox
{
    public static class EnumFromNative
    {
        public static Value Create(Type enumType)
        {
            if (!enumType.IsEnum)
                throw new UloxException($"Cannot create enum mapping for Type '{enumType.Name}' is not an enum.");

            var nameHashedString = new HashedString(enumType.Name);
            var enumClass = new EnumClass(nameHashedString);
            var returnEnumBinding = Value.New(enumClass);
            var enumNames = enumType.GetEnumNames();
            var enumValues = (int[])enumType.GetEnumValues();
            foreach (var (k, v) in enumNames.Select((x, i) => (x, enumValues[i])))
            {
                enumClass.AddEnumValue(Value.New(k), Value.New(v));
            }

            return returnEnumBinding;
        }
    }
}
