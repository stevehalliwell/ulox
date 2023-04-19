using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ULox
{
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

        
        public ValueType type;
        public ValueTypeDataUnion val;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsFalsey() => type == ValueType.Null || (type == ValueType.Bool && !val.asBool);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNull() => type == ValueType.Null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string str() => ToString();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                    return "<Null Chunk in Value.ToString. Illegal.>";
                var name = chunk.Name;
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
                
            case ValueType.CombinedClosures:
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
            case ValueType.CombinedClosures:
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
            => new Value() { type = valueType, val = dataUnion };

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
            return New(ValueType.NativeFunction, new ValueTypeDataUnion() { asNativeFunc = val , asByte0 = returnCount, asByte1 = argCount});
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
            => new Value() { type = ValueType.Null };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Value Object(object obj) 
            => New(ValueType.Object, new ValueTypeDataUnion() { asObject = obj });

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Value Combined()
            => New(ValueType.CombinedClosures, new ValueTypeDataUnion() { asObject = new List<ClosureInternal>() });

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
        public static bool operator ==(Value left, Value right)
            => left.Equals(ref right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Value left, Value right)
            => !(left == right);

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
                case ValueType.CombinedClosures:
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
            case ValueType.CombinedClosures:
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
            case ValueType.CombinedClosures:
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Value UpdateFrom(Value lhs, Value rhs, Vm vm)
        {
            if (rhs.type != lhs.type)
            {
                return lhs;
            }

            switch (lhs.type)
            {
            case ValueType.BoundMethod:
            case ValueType.UserType:
            case ValueType.Upvalue:
            case ValueType.CombinedClosures:
            case ValueType.Closure:
            case ValueType.NativeFunction:
            case ValueType.Chunk:
            case ValueType.Null:
            case ValueType.Object:
                lhs = rhs;
                break;
            case ValueType.Double:
                lhs.val.asDouble = rhs.val.asDouble;
                break;
            case ValueType.Bool:
                lhs.val.asBool = rhs.val.asBool;
                break;
            case ValueType.String:
                lhs.val.asString = rhs.val.asString;
                break;
            case ValueType.Instance:
                if(lhs.val.asInstance is INativeCollection lhsNativeCol
                    && rhs.val.asInstance is INativeCollection rhsNativeCol
                    && lhsNativeCol.GetType() == rhsNativeCol.GetType())
                {
                    //we could do internal changes but we end up just doing this more long form
                    lhs = rhs;
                }
                else
                {
                    //deal with regular field updates
                    var lhsInst = lhs.val.asInstance;
                    foreach (var field in lhsInst.Fields)
                    {
                        if (rhs.val.asInstance.Fields.TryGetValue(field.Key, out var rhsField))
                        {
                            lhsInst.Fields[field.Key] = UpdateFrom(field.Value, rhsField, vm);
                        }
                    }
                }
                break;
            default:
                vm.ThrowRuntimeException($"Unhandled value type '{lhs.type}' in update, with lhs '{lhs}' and rhs '{rhs}'");
                break;
            }

            return lhs;
        }

        public bool IsCallableWithArity(int arity)
        {
            switch (this.type)
            {
            case ValueType.Null:
            case ValueType.Double:
            case ValueType.Bool:
            case ValueType.Upvalue:
            case ValueType.String:
            case ValueType.UserType:
            case ValueType.Instance:
            case ValueType.Object:
                return false;
            case ValueType.Chunk:
                return val.asChunk.Arity == arity;
            case ValueType.NativeFunction:
                return true;//hope so
            case ValueType.Closure:
                return val.asClosure.chunk.Arity == arity;
            case ValueType.CombinedClosures:
                return val.asCombined[0].chunk.Arity == arity;
            case ValueType.BoundMethod:
                return val.asBoundMethod.Method.chunk.Arity == arity;

            default:
                throw new UloxException($"Unhandled value type '{this.type}' in {nameof(IsCallableWithArity)}");
            }
        }
    }
}
