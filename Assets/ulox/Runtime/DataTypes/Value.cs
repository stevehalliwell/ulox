namespace ULox
{
    public struct Value
    {
        public ValueType type;
        public ValueTypeDataUnion val;

        public bool IsFalsey => type == ValueType.Null || (type == ValueType.Bool && !val.asBool);

        public bool IsNull => type == ValueType.Null;
        
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
                return val.asString?.ToString() ?? "null";
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

        public static Value New(ValueType valueType, ValueTypeDataUnion dataUnion)
            => new Value() { type = valueType, val = dataUnion };

        public static Value New(double val) => New( ValueType.Double,new ValueTypeDataUnion() { asDouble = val});

        public static Value New(bool val) => New( ValueType.Bool, new ValueTypeDataUnion() { asBool = val });

        public static Value New(string val) => New(ValueType.String, new ValueTypeDataUnion() { asString = val });

        public static Value New(Chunk val) => New(ValueType.Chunk, new ValueTypeDataUnion() { asObject = val } );

        public static Value New(System.Func<VM, int, Value> val) 
            => New( ValueType.NativeFunction, new ValueTypeDataUnion() { asNativeFunc = val });

        public static Value New(ClosureInternal val)
        { 
            var res = New( ValueType.Closure, new ValueTypeDataUnion() { asObject = val });
            val.upvalues = new Value[val.chunk.UpvalueCount];
            return res;
        }

        public static Value New(UpvalueInternal val) => New( ValueType.Upvalue, new ValueTypeDataUnion() { asObject = val });

        public static Value New(ClassInternal val) => New(ValueType.Class,new ValueTypeDataUnion() { asObject = val } );

        public static Value New(InstanceInternal val) => New( ValueType.Instance, new ValueTypeDataUnion() { asObject = val });

        public static Value New(BoundMethod val) => New(ValueType.BoundMethod, new ValueTypeDataUnion() { asObject = val });

        public static Value Null() => new Value() { type = ValueType.Null };

        public static Value Object(object obj) => New( ValueType.Object, new ValueTypeDataUnion() { asObject = obj });
    }
}
