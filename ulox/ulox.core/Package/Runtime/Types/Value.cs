using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Collections;
using System;

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
                return val.asString.String ?? "null";

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
                return $"<class {val.asClass.Name}>";

            case ValueType.Instance:
                return $"<inst {val.asInstance.FromClass?.Name}>";

            case ValueType.BoundMethod:
                return $"<boundMeth {val.asBoundMethod.Method.chunk.Name}>";

            case ValueType.Object:
                if (val.asObject is TypeNameClass typenameClass)
                    return typenameClass.ToString();
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
                var newInst = new InstanceInternal();
                newInst.CopyFrom(inst);
                return Value.New(newInst);
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

        public static Value New(double val) 
            => New(ValueType.Double, new ValueTypeDataUnion() { asDouble = val });

        public static Value New(bool val) 
            => New(ValueType.Bool, new ValueTypeDataUnion() { asBool = val });

        public static Value New(HashedString val) 
            => New(ValueType.String, new ValueTypeDataUnion() { asString = val });

        public static Value New(string val) 
            => New(new HashedString(val));

        public static Value New(Chunk val) 
            => New(ValueType.Chunk, new ValueTypeDataUnion() { asObject = val });

        public static Value New(Vm.NativeCallDelegate val)
            => New(ValueType.NativeFunction, new ValueTypeDataUnion() { asNativeFunc = val });

        public static Value New(ClosureInternal val)
        {
            var res = New(ValueType.Closure, new ValueTypeDataUnion() { asObject = val });
            val.upvalues = new Value[val.chunk.UpvalueCount];
            return res;
        }

        public static Value New(UpvalueInternal val) 
            => New(ValueType.Upvalue, new ValueTypeDataUnion() { asObject = val });

        public static Value New(ClassInternal val) 
            => New(ValueType.Class, new ValueTypeDataUnion() { asObject = val });

        public static Value New(InstanceInternal val) 
            => New(ValueType.Instance, new ValueTypeDataUnion() { asObject = val });

        public static Value New(BoundMethod val) 
            => New(ValueType.BoundMethod, new ValueTypeDataUnion() { asObject = val });

        public static Value Null() 
            => new Value() { type = ValueType.Null };

        public static Value Object(object obj) 
            => New(ValueType.Object, new ValueTypeDataUnion() { asObject = obj });

        public static Value Combined()
            => New(ValueType.CombinedClosures, new ValueTypeDataUnion() { asObject = new List<ClosureInternal>() });

        public override bool Equals(object obj)
        {
            var asValue = (Value)obj;
            return Equals(ref asValue);
        }

        public bool Equals(ref Value rhs)
            => Compare(ref this, ref rhs);

        public static bool operator ==(Value left, Value right)
            => left.Equals(ref right);

        public static bool operator !=(Value left, Value right)
            => !(left == right);

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

                case ValueType.Instance:
                    return lhs.val.asInstance == rhs.val.asInstance;

                case ValueType.Class:
                    return lhs.val.asClass == rhs.val.asClass;

                case ValueType.Object:
                    return lhs.val.asObject.Equals(rhs.val.asObject);

                default:
                    throw new VMException($"Cannot perform compare on type '{lhs.type}'.");
                }
            }
        }

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
            case ValueType.Class:
            case ValueType.Instance:
            case ValueType.BoundMethod:
            case ValueType.Object:
            default:
                return EqualityComparer<object>.Default.GetHashCode(val);
            }
        }

        public Value GetLoxClassType()
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
            case ValueType.CombinedClosures:
            case ValueType.BoundMethod:
            case ValueType.Upvalue:
                break;
            case ValueType.Class:
                return Value.New(val.asClass);
            case ValueType.Instance:
                return Value.New(val.asInstance.FromClass);
            case ValueType.Object:
                return Value.Object(new TypeNameClass("UserObject"));
            default:
                break;
            }
            return Value.Null();
        }
        
        public sealed class TypeNameClass : IEquatable<TypeNameClass>
        {
            private string _typename;

            public TypeNameClass(string typename)
            {
                _typename = typename;
            }

            public override bool Equals(object obj)
            {
                return obj is TypeNameClass customDummyClass 
                    ? Equals(customDummyClass)
                    : false;
            }

            public bool Equals(TypeNameClass other)
            {
                return _typename == other._typename;
            }

            public override int GetHashCode()
            {
                return -776812171 + EqualityComparer<string>.Default.GetHashCode(_typename);
            }

            public override string ToString() => $"<class {_typename}>";
        }

        public bool IsPure 
        { 
            get
            {
                switch (type)
                {
                case ValueType.Null:
                case ValueType.Double:
                case ValueType.Bool:
                case ValueType.String:
                case ValueType.Class:
                case ValueType.Instance:
                    return true;
                case ValueType.Chunk:
                    return val.asChunk.FunctionType == FunctionType.PureFunction;
                case ValueType.Closure:
                    return val.asClosure.chunk.FunctionType == FunctionType.PureFunction;
                case ValueType.BoundMethod:
                    return val.asBoundMethod.Method.chunk.FunctionType == FunctionType.PureFunction;
                case ValueType.CombinedClosures:
                    return val.asCombined.Select(x => x.chunk.FunctionType == FunctionType.PureFunction).Any();
                case ValueType.NativeFunction:  //todo nativefuncs could declare themselves to be pure
                case ValueType.Upvalue:
                case ValueType.Object:
                    return false;
                default:
                    return false;
                }
            } 
        }
    }

    public class ValueComparer : IComparer<Value>
    {
        public readonly static ValueComparer Instance = new ValueComparer();

        public int Compare(Value x, Value y)
        {
            switch (x.type)
            {
            case ValueType.Null:
                return 0;
            case ValueType.Double:
                return x.val.asDouble.CompareTo(y.val.asDouble);
            case ValueType.Bool:
                return x.val.asBool.CompareTo(y.val.asBool);
            case ValueType.String:
                return x.val.asString.CompareTo(y.val.asString);
            case ValueType.Chunk:
            case ValueType.NativeFunction:
            case ValueType.Closure:
            case ValueType.CombinedClosures:
            case ValueType.Upvalue:
            case ValueType.Class:
            case ValueType.Instance:
            case ValueType.BoundMethod:
            case ValueType.Object:
                return Comparer.Default.Compare(x.val.asObject, y.val.asObject);
            default:
                throw new Exception();
            }
        }
    }
}
