using System.Runtime.CompilerServices;

namespace ULox
{
    public struct Value
    {
        public ValueType type;
        public ValueTypeDataUnion val;

        public bool IsFalsey => type == ValueType.Null || (type == ValueType.Bool && !val.asBool);

        public bool IsNull => type == ValueType.Null;

        public string str() => ToString();

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
                    return val.asString ?? "null";

                case ValueType.Chunk:
                    var chunk = val.asChunk;
                    if (chunk == null)
                        throw new System.Exception("Null Chunk in Value.ToString. Illegal.");
                    var name = chunk.Name;
                    return "<fn " + name + "> ";

                case ValueType.NativeFunction:
                    return "<NativeFunc>";

                case ValueType.Closure:
                    return $"<closure {val.asClosure.chunk.Name} upvals:{val.asClosure.upvalues.Length}>";

                case ValueType.Upvalue:
                    return $"<upvalue {val.asUpvalue.index}>";

                case ValueType.Class:
                    return $"<class {val.asClass.name}>";

                case ValueType.Instance:
                    return $"<inst {val.asInstance.fromClass?.name}>";

                case ValueType.BoundMethod:
                    return $"<boundMeth {val.asBoundMethod.method.chunk.Name}>";

                case ValueType.Object:
                    return $"<object {val.asObject}>";

                default:
                throw new System.NotImplementedException();
            }
        }

        public static Value Copy(Value copyFrom)
        {
            switch (copyFrom.type)
            {
            case ValueType.Instance:
                var inst = copyFrom.val.asInstance;
                return Value.New(new InstanceInternal()
                {
                    fields = new Table(inst.fields),
                    fromClass = inst.fromClass
                });
                break;
            case ValueType.Null:
            case ValueType.Double:
            case ValueType.Bool:
            case ValueType.String:
            case ValueType.Chunk:
            case ValueType.NativeFunction:
            case ValueType.Closure:
            case ValueType.Upvalue:
            case ValueType.Class:
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

        public static Value New(ValueType valueType, ValueTypeDataUnion dataUnion)
            => new Value() { type = valueType, val = dataUnion };

        public static Value New(double val) => New(ValueType.Double, new ValueTypeDataUnion() { asDouble = val });

        public static Value New(bool val) => New(ValueType.Bool, new ValueTypeDataUnion() { asBool = val });

        public static Value New(string val) => New(ValueType.String, new ValueTypeDataUnion() { asString = val });

        public static Value New(Chunk val) => New(ValueType.Chunk, new ValueTypeDataUnion() { asObject = val });

        public static Value New(System.Func<VMBase, int, Value> val)
            => New(ValueType.NativeFunction, new ValueTypeDataUnion() { asNativeFunc = val });

        public static Value New(ClosureInternal val)
        {
            var res = New(ValueType.Closure, new ValueTypeDataUnion() { asObject = val });
            val.upvalues = new Value[val.chunk.UpvalueCount];
            return res;
        }

        public static Value New(UpvalueInternal val) => New(ValueType.Upvalue, new ValueTypeDataUnion() { asObject = val });

        public static Value New(ClassInternal val) => New(ValueType.Class, new ValueTypeDataUnion() { asObject = val });

        public static Value New(InstanceInternal val) => New(ValueType.Instance, new ValueTypeDataUnion() { asObject = val });

        public static Value New(BoundMethod val) => New(ValueType.BoundMethod, new ValueTypeDataUnion() { asObject = val });

        public static Value Null() => new Value() { type = ValueType.Null };

        public static Value Object(object obj) => New(ValueType.Object, new ValueTypeDataUnion() { asObject = obj });

        public override bool Equals(object obj)
        {
            var asValue = (Value)obj;
            return Equals(ref asValue);
        }

        public bool Equals(ref Value rhs)
        {
            return Compare(ref this, ref rhs);
        }

        public static bool operator ==(Value left, Value right)
        {
            return left.Equals(ref right);
        }

        public static bool operator !=(Value left, Value right)
        {
            return !(left == right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Compare(ref Value lhs, ref Value rhs)
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

                    default:
                    throw new VMException($"Cannot perform compare on type '{lhs.type}'.");
                }
            }
        }
    }
}
