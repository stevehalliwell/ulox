using System.Collections.Generic;

namespace ULox
{
    public sealed class EnumValue
    {
        public Value Key;
        public Value Value;
        public UserTypeInternal FromEnum;

        public EnumValue(Value key, Value value, UserTypeInternal fromEnum)
        {
            Key = key;
            Value = value;
            FromEnum = fromEnum;
        }

        public override bool Equals(object obj)
        {
            return obj is EnumValue value &&
                   EqualityComparer<Value>.Default.Equals(Key, value.Key) &&
                   EqualityComparer<Value>.Default.Equals(Value, value.Value) &&
                   EqualityComparer<InstanceInternal>.Default.Equals(FromEnum, value.FromEnum);
        }

        public override int GetHashCode()
        {
            int hashCode = 1470372950;
            hashCode = hashCode * -1521134295 + Key.GetHashCode();
            hashCode = hashCode * -1521134295 + Value.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<InstanceInternal>.Default.GetHashCode(FromEnum);
            return hashCode;
        }

        public override string ToString()
        {
            return $"<{nameof(EnumValue)} {FromEnum.Name}.{Key} ({Value})>";
        }
    }
}
