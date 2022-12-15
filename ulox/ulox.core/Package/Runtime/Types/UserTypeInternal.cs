using System.Collections.Generic;
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

        public static readonly Dictionary<OpCode, int> OpCodeToOverloadIndex = new Dictionary<OpCode, int>()
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

        private readonly Table methods = new Table();
        private readonly Table flavours = new Table();
        private readonly Value[] overloadableOperators = new Value[OverloadableMethodNames.Length];

        //TODO these props also need to be write protected by the freeze
        public HashedString Name { get; protected set; }

        public UserType UserType { get; }
        public Value Initialiser { get; protected set; } = Value.Null();
        public List<(ClosureInternal closure, ushort instruction)> InitChains { get; protected set; } = new List<(ClosureInternal, ushort)>();
        public IReadOnlyDictionary<HashedString, Value> Methods => methods.AsReadOnly;
        public IReadOnlyList<HashedString> FieldNames => _fieldsNames;
        private List<HashedString> _fieldsNames = new List<HashedString>();

        public UserTypeInternal(HashedString name, UserType userType)
        {
            Name = name;
            UserType = userType;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual InstanceInternal MakeInstance()
        {
            return new InstanceInternal(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Value GetMethod(HashedString name) => methods[name];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetMethod(HashedString name, out Value method) => methods.TryGetValue(name, out method);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddMethod(HashedString key, Value method)
        {
            // This is used internally by the vm only does not need to check for frozen

            methods[key] = method;
            if (key == TypeCompilette.InitMethodName)
            {
                Initialiser = method;
            }
            var opIndex = System.Array.FindIndex(OverloadableMethodNames, x => key.Hash == x.Hash);
            if (opIndex != -1)
            {
                overloadableOperators[opIndex] = method;
            }
        }

        public void AddInitChain(ClosureInternal closure, ushort initChainStartOp)
        {
            // This is used internally by the vm only does not need to check for frozen

            InitChains.Add((closure, initChainStartOp));
        }

        public void AddMixin(Value flavourValue, Vm vm)
        {
            // This is used internally by the vm only does not need to check for frozen

            var flavour = flavourValue.val.asClass;
            flavours[flavour.Name] = flavourValue;

            foreach (var flavourMeth in flavour.methods)
            {
                MixinMethod(flavourMeth.Key, flavourMeth.Value, vm);
            }

            foreach (var flavourInitChain in flavour.InitChains)
            {
                if (!InitChains.Contains(flavourInitChain))
                {
                    AddInitChain(flavourInitChain.closure, flavourInitChain.instruction);
                }
            }
        }

        private void MixinMethod(HashedString key, Value value, Vm vm)
        {
            if (methods.TryGetValue(key, out var existing))
            {
                //combine
                if (existing.type == ValueType.Closure)
                {
                    var existingArity = existing.val.asClosure.chunk.Arity;
                    var newArity = value.val.asClosure.chunk.Arity;
                    if (existingArity != newArity)
                    {
                        vm.ThrowRuntimeException($"Cannot mixin method '{key}' as it has a different arity '{newArity}' to the existing method '{existingArity}'.");
                    }

                    //make a combine
                    var temp = Value.Combined();
                    temp.val.asCombined.Add(existing.val.asClosure);
                    temp.val.asCombined.Add(value.val.asClosure);
                    existing = temp;
                }
                else
                {
                    var existingArity = existing.val.asCombined[0].chunk.Arity;
                    var newArity = value.val.asClosure.chunk.Arity;
                    if (existingArity != newArity)
                    {
                        vm.ThrowRuntimeException($"Cannot mixin method '{key}' as it has a different arity '{newArity}' to the existing method '{existingArity}'.");
                    }

                    existing.val.asCombined.Add(value.val.asClosure);
                }

                value = existing;
            }

            AddMethod(key, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Value GetOverloadClosure(OpCode opCode)
        {
            return overloadableOperators[OpCodeToOverloadIndex[opCode]];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void FinishCreation(InstanceInternal inst)
            => inst.Freeze();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddFieldName(HashedString fieldName)
            => _fieldsNames.Add(fieldName);

        public override string ToString() => $"<{nameof(UserTypeInternal)}:{Name}>";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddEnumValue(Value key, Value val)
        {
            Fields[key.val.asString] = val;
        }
    }
}
