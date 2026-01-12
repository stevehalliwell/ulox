using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ULox
{
    [StructLayout(LayoutKind.Explicit)]
    public struct ValueTypeDataUnion
    {
        [FieldOffset(8 * 0)]
        public double asDouble;

        [FieldOffset(8 * 0)]
        public bool asBool;

        [FieldOffset(8 * 0 + 0)]
        public byte asByte0;
        [FieldOffset(8 * 0 + 1)]
        public byte asByte1;
        [FieldOffset(8 * 0 + 2)]
        public byte asByte2;
        [FieldOffset(8 * 0 + 3)]
        public byte asByte3;

        [FieldOffset(8 * 1)]
        public HashedString asString;

        [FieldOffset(8 * 1)]
        public CallFrame.NativeCallDelegate asNativeFunc;

        [FieldOffset(8 * 1)]
        public object asObject;

        public ClosureInternal asClosure => asObject as ClosureInternal;
        public UpvalueInternal asUpvalue => asObject as UpvalueInternal;
        public UserTypeInternal asClass => asObject as UserTypeInternal;
        public InstanceInternal asInstance => asObject as InstanceInternal;
        public Chunk asChunk => asObject as Chunk;
        public BoundMethod asBoundMethod => asObject as BoundMethod;
    }
    public enum ValueType : byte
    {
        Null,
        Double,
        Bool,
        String,
        Chunk,
        NativeFunction,
        Closure,
        Upvalue,
        UserType,
        Instance,
        BoundMethod,
        Object,
    }

    public interface INativeCollection
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Value Get(Value ind);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Set(Value ind, Value val);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Value Count();
    }

    public struct Value
    {
        public sealed class TypeNameClass : System.IEquatable<TypeNameClass>
        {
            private readonly string _typename;

            public TypeNameClass(string typename)
            {
                _typename = typename;
            }

            public override bool Equals(object obj)
            {
                return obj is TypeNameClass customDummyClass
                    && Equals(customDummyClass);
            }

            public bool Equals(TypeNameClass other)
            {
                return _typename == other._typename;
            }

            public override int GetHashCode()
            {
                return -776812171 + EqualityComparer<string>.Default.GetHashCode(_typename);
            }

            public override string ToString() => $"<{UserType.Native} {_typename}>";
        }


        public ValueTypeDataUnion val;
        public ValueType type;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsFalsey() => type == ValueType.Null || (type == ValueType.Bool && !val.asBool);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNull() => type == ValueType.Null;

        public override string ToString()
        {
            switch (type)
            {
            case ValueType.Null:
                return "null";

            case ValueType.Double:
                return val.asDouble.ToString();

            case ValueType.Bool:
                return val.asBool.ToString();

            case ValueType.String:
                return val.asString.String ?? "null";

            case ValueType.Chunk:
                var chunk = val.asChunk;
                var name = chunk.ChunkName ?? "null";
                return "<fn " + name + "> ";

            case ValueType.NativeFunction:
                return "<NativeFunc>";

            case ValueType.Upvalue:
                return $"<upvalue {val.asUpvalue.index}>";

            case ValueType.Closure:
            case ValueType.UserType:
            case ValueType.Instance:
            case ValueType.BoundMethod:
                return val.asObject.ToString();

            case ValueType.Object:
                if (val.asObject is TypeNameClass typenameClass)
                    return typenameClass.ToString();
                return $"<object {val.asObject}>";

            default:
                throw new System.NotImplementedException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Value Copy(Value copyFrom)
        {
            switch (copyFrom.type)
            {
            case ValueType.Instance:
                var inst = copyFrom.val.asInstance;
                var newInst = new InstanceInternal();
                newInst.CopyFrom(inst);
                return Value.New(newInst);
            case ValueType.Null:
            case ValueType.Double:
            case ValueType.Bool:
            case ValueType.String:
            case ValueType.Chunk:
            case ValueType.NativeFunction:
            case ValueType.Closure:
            case ValueType.Upvalue:
            case ValueType.UserType:
            case ValueType.BoundMethod:
            case ValueType.Object:
            default:
                return new Value()
                {
                    type = copyFrom.type,
                    val = copyFrom.val
                };
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Value New(ValueType valueType, ValueTypeDataUnion dataUnion)
            => new() { type = valueType, val = dataUnion };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Value New(double val)
            => New(ValueType.Double, new ValueTypeDataUnion() { asDouble = val });

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Value New(bool val)
            => New(ValueType.Bool, new ValueTypeDataUnion() { asBool = val });

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Value New(HashedString val)
            => New(ValueType.String, new ValueTypeDataUnion() { asString = val });

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Value New(string val)
            => New(new HashedString(val));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Value New(Chunk val)
            => New(ValueType.Chunk, new ValueTypeDataUnion() { asObject = val });

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Value New(CallFrame.NativeCallDelegate val, byte returnCount, byte argCount)
        {
            return New(ValueType.NativeFunction, new ValueTypeDataUnion() { asNativeFunc = val, asByte0 = returnCount, asByte1 = argCount });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Value New(ClosureInternal val)
        {
            var res = New(ValueType.Closure, new ValueTypeDataUnion() { asObject = val });
            val.upvalues = new Value[val.chunk.UpvalueCount];
            return res;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Value New(UpvalueInternal val)
            => New(ValueType.Upvalue, new ValueTypeDataUnion() { asObject = val });

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Value New(UserTypeInternal val)
            => New(ValueType.UserType, new ValueTypeDataUnion() { asObject = val });

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Value New(InstanceInternal val)
            => New(ValueType.Instance, new ValueTypeDataUnion() { asObject = val });

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Value New(BoundMethod val)
            => New(ValueType.BoundMethod, new ValueTypeDataUnion() { asObject = val });

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Value Null()
            => new() { type = ValueType.Null };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Value Object(object obj)
            => New(ValueType.Object, new ValueTypeDataUnion() { asObject = obj });

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            var asValue = (Value)obj;
            return Equals(ref asValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ref Value rhs)
            => Compare(ref this, ref rhs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Compare(ref Value lhs, ref Value rhs)
        {
            if (lhs.type != rhs.type)
            {
                return false;
            }
            else
            {
                switch (lhs.type)
                {
                case ValueType.Null:
                    return true;

                case ValueType.Double:
                    return lhs.val.asDouble == rhs.val.asDouble;

                case ValueType.Bool:
                    return lhs.val.asBool == rhs.val.asBool;

                case ValueType.String:
                    return lhs.val.asString == rhs.val.asString;

                case ValueType.Instance:
                    return lhs.val.asInstance.Equals(rhs.val.asInstance);

                case ValueType.UserType:
                    return lhs.val.asClass == rhs.val.asClass;

                case ValueType.Object:
                    return lhs.val.asObject.Equals(rhs.val.asObject);

                case ValueType.Closure:
                    return lhs.val.asClosure == rhs.val.asClosure;

                case ValueType.NativeFunction:
                case ValueType.Upvalue:
                case ValueType.BoundMethod:
                case ValueType.Chunk:
                default:
                    throw new UloxException($"Cannot perform compare on type '{lhs.type}'.");
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            switch (type)
            {
            case ValueType.Double:
                return val.asDouble.GetHashCode();

            case ValueType.Bool:
                return val.asBool.GetHashCode();

            case ValueType.String:
                return val.asString.Hash;
            case ValueType.Null:
            case ValueType.Chunk:
            case ValueType.NativeFunction:
            case ValueType.Closure:
            case ValueType.Upvalue:
            case ValueType.UserType:
            case ValueType.Instance:
            case ValueType.BoundMethod:
            case ValueType.Object:
            default:
                return EqualityComparer<object>.Default.GetHashCode(val);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Value GetClassType()
        {
            switch (type)
            {
            case ValueType.Null:
                return Value.Null();
            case ValueType.Double:
                return Value.Object(new TypeNameClass("Number"));
            case ValueType.Bool:
                return Value.Object(new TypeNameClass("Bool"));
            case ValueType.String:
                return Value.Object(new TypeNameClass("String"));
            case ValueType.Chunk:
            case ValueType.NativeFunction:
            case ValueType.Closure:
            case ValueType.BoundMethod:
            case ValueType.Upvalue:
                break;
            case ValueType.UserType:
                return Value.New(val.asClass);
            case ValueType.Instance:
                return Value.New(val.asInstance.FromUserType);
            case ValueType.Object:
                return Value.Object(new TypeNameClass("UserObject"));
            default:
                break;
            }
            return Value.Null();
        }
    }
}
