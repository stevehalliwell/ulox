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
        private readonly Table flavours = new Table();
        private readonly Value[] overloadableOperators = new Value[OverloadableMethodNames.Length];
        
        public HashedString Name { get; protected set; }

        public UserType UserType { get; }
        public Value Initialiser { get; protected set; } = Value.Null();
        public List<(Chunk chunk, ushort instruction)> InitChains { get; protected set; } = new List<(Chunk, ushort)>();
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

        public void PreareFromType(Vm vm)
        {
            foreach (var field in _typeInfoEntry.Fields)
            {
                AddFieldName(new HashedString(field));
            }

            foreach (var (chunk, labelID) in _typeInfoEntry.InitChains)
            {
                var loc = (ushort)chunk.Labels[labelID];
                if (loc != 0)
                {
                    AddInitChain(chunk, loc);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual InstanceInternal MakeInstance()
        {
            return new InstanceInternal(this);
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

        public void AddInitChain(Chunk chunk, ushort initChainStartOp)
        {
            // This is used internally by the vm only does not need to check for frozen

            InitChains.Add((chunk, initChainStartOp));
        }
        
        public void MixinClass(Value flavourValue, Vm vm)
        {
            var flavour = flavourValue.val.asClass;
            ValidateMixin(flavour, vm);

            flavours.AddOrSet(flavour.Name, flavourValue);

            foreach (var flavourMeth in flavour.Methods)
            {
                AddMethod(flavourMeth.Key, flavourMeth.Value, vm);
            }

            foreach (var flavourInitChain in flavour.InitChains)
            {
                if (!InitChains.Contains(flavourInitChain))
                {
                    AddInitChain(flavourInitChain.chunk, flavourInitChain.instruction);
                }
            }

            foreach (var fieldName in flavour.FieldNames)
            {
                if (_fieldsNames.Contains(fieldName))
                    continue;

                AddFieldName(fieldName);
            }
        }

        private void ValidateMixin(UserTypeInternal flavour, Vm vm)
        {
            switch (UserType)
            {
            case UserType.Class:
                break;
            case UserType.Enum:
            case UserType.Native:
            default:
                vm.ThrowRuntimeException($"Encounted unexpected mixin type on type '{this.Name}'");
                break;
            }
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

        public override string ToString() => $"<{UserType} {Name}>";
    }
}
