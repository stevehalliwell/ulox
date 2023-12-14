using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ULox
{
    public class UserTypeInternal : InstanceInternal
    {
        public static readonly HashedString[] OverloadableMethodNames = new HashedString[]
        {
            new HashedString("_add"),
            new HashedString("_sub"),
            new HashedString("_mul"),
            new HashedString("_div"),
            new HashedString("_mod"),
            new HashedString("_eq"),
            new HashedString("_ls"),
            new HashedString("_gr"),
            new HashedString("_gi"),
            new HashedString("_si"),
            new HashedString("_co"),
        };

        public static readonly IReadOnlyDictionary<OpCode, int> OpCodeToOverloadIndex = new Dictionary<OpCode, int>()
        {
            {OpCode.ADD,        0 },
            {OpCode.SUBTRACT,   1 },
            {OpCode.MULTIPLY,   2 },
            {OpCode.DIVIDE,     3 },
            {OpCode.MODULUS,    4 },
            {OpCode.EQUAL,      5 },
            {OpCode.LESS,       6 },
            {OpCode.GREATER,    7 },
            {OpCode.GET_INDEX,  8 },
            {OpCode.SET_INDEX,  9 },
            {OpCode.COUNT_OF,   10 },
        };

        public Table Methods { get; private set; } = new Table();
        private readonly Value[] overloadableOperators = new Value[OverloadableMethodNames.Length];

        public HashedString Name { get; protected set; }

        public UserType UserType { get; }
        public Value Initialiser { get; protected set; } = Value.Null();
        public List<(Chunk chunk, byte labelID)> InitChains { get; protected set; } = new List<(Chunk, byte)>();
        public IReadOnlyList<HashedString> FieldNames => _fieldsNames;
        private readonly List<HashedString> _fieldsNames = new List<HashedString>();
        protected TypeInfoEntry _typeInfoEntry;

        public UserTypeInternal(HashedString name, UserType userType)
        {
            Name = name;
            UserType = userType;
        }

        public UserTypeInternal(TypeInfoEntry type)
        {
            _typeInfoEntry = type;

            Name = new HashedString(type.Name);
            UserType = type.UserType;
        }

        public void PrepareFromType(Vm vm)
        {
            foreach (var field in _typeInfoEntry.Fields)
            {
                AddFieldName(new HashedString(field));
            }

            foreach (var staticField in _typeInfoEntry.StaticFields)
            {
                Fields.AddOrSet(new HashedString(staticField), Value.Null());
            }

            foreach (var (chunk, loc) in _typeInfoEntry.InitChains)
            {
                AddInitChain(chunk, loc);
            }

            foreach (var method in _typeInfoEntry.Methods)
            {
                var methodValue = Value.New(new ClosureInternal { chunk = method });
                AddMethod(new HashedString(method.Name), methodValue, vm);
            }

            Freeze();
            if (UserType == UserType.Enum)
                ReadOnly();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual InstanceInternal MakeInstance()
        {
            var ret = new InstanceInternal(this);

            foreach (var fieldName in _fieldsNames)
            {
                ret.SetField(fieldName, Value.Null());
            }

            ret.Freeze();
            return ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddMethod(HashedString key, Value method, Vm vm)
        {
            // This is used internally by the vm only does not need to check for frozen
            if (Methods.Get(key, out var existing))
            {
                //combine
                if (existing.type == ValueType.Closure)
                {
                    var existingArity = existing.val.asClosure.chunk.Arity;
                    var newArity = method.val.asClosure.chunk.Arity;
                    if (existingArity != newArity)
                        vm.ThrowRuntimeException($"Cannot mixin method '{key}' as it has a different arity '{newArity}' to the existing method '{existingArity}'.");

                    //make a combine
                    var temp = Value.Combined();
                    temp.val.asCombined.Add(method.val.asClosure);
                    temp.val.asCombined.Add(existing.val.asClosure);
                    existing = temp;
                }
                else
                {
                    var existingArity = existing.val.asCombined[0].chunk.Arity;
                    var newArity = method.val.asClosure.chunk.Arity;
                    if (existingArity != newArity)
                        vm.ThrowRuntimeException($"Cannot mixin method '{key}' as it has a different arity '{newArity}' to the existing method '{existingArity}'.");

                    existing.val.asCombined.Insert(0, method.val.asClosure);
                }

                method = existing;
            }

            Methods.AddOrSet(key, method);

            if (key == ClassTypeCompilette.InitMethodName)
            {
                Initialiser = method;
            }
            var opIndex = System.Array.FindIndex(OverloadableMethodNames, x => key.Hash == x.Hash);
            if (opIndex != -1)
            {
                overloadableOperators[opIndex] = method;
            }
        }

        public void AddInitChain(Chunk chunk, byte labelID)
        {
            // This is used internally by the vm only does not need to check for frozen
            if (InitChains.Any(x => x.chunk == chunk)) return;

            InitChains.Add((chunk, labelID));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Value GetOverloadClosure(OpCode opCode)
        {
            return overloadableOperators[OpCodeToOverloadIndex[opCode]];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddFieldName(HashedString fieldName)
            => _fieldsNames.Add(fieldName);

        public override string ToString() => $"<{UserType} {Name}>";
    }
}
