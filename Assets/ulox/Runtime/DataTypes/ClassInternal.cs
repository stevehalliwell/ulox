using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ULox
{
    public class ClassInternal : InstanceInternal
    {
        public const int FirstMathOp = (int)OpCode.ADD;
        public const int LastMathOp = (int)OpCode.MODULUS;

        public readonly static List<string> MathOperatorMethodNames = new List<string>()
        {
            "_add",
            "_sub",
            "_mul",
            "_div",
            "_mod",
        };

        public const int FirstCompOp = (int)OpCode.EQUAL;
        public const int LastCompOp = (int)OpCode.GREATER;

        public readonly static List<string> ComparisonOperatorMethodNames = new List<string>()
        {
            "_eq",
            "_ls",
            "_gr",
        };

        private readonly Dictionary<string, Value> methods = new Dictionary<string, Value>();
        private readonly Dictionary<string, ClassInternal> flavours = new Dictionary<string, ClassInternal>();
        private readonly Value[] mathOperators = new Value[LastMathOp - FirstMathOp + 1];
        private readonly Value[] compOperators = new Value[LastCompOp - FirstCompOp + 1];

        //TODO these props also need to be write protected by the freeze
        public string Name { get; protected set; }
        public Value Initialiser { get; protected set; } = Value.Null();
        public ClassInternal Super { get; protected set; }
        public List<(ClosureInternal, int)> InitChains { get; protected set; } = new List<(ClosureInternal, int)>();

        public ClassInternal(string name)
        {
            Name = name;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Value GetMethod(string name) => methods[name];

        public bool TryGetMethod(string name, out Value method) => methods.TryGetValue(name, out method);

        public void AddMethod(string key, Value method)
        {
            CanWrite();
            methods[key] = method;
            if (key == ClassCompilette.InitMethodName)
            {
                Initialiser = method;
            }
            var opIndex = MathOperatorMethodNames.IndexOf(key);
            if (opIndex != -1)
            {
                mathOperators[opIndex] = method;
            }
            else
            {
                opIndex = ComparisonOperatorMethodNames.IndexOf(key);
                if (opIndex != -1)
                {
                    compOperators[opIndex] = method;
                }
            }
        }

        public void CanWrite()
        {
            if (IsFrozen)
                throw new FreezeException($"Attempted to modify frozen class '{Name}'.");
        }

        public void AddInitChain(ClosureInternal closure, ushort initChainStartOp)
        {
            CanWrite();
            InitChains.Add((closure, initChainStartOp));
        }

        public void InheritFrom(ClassInternal superClass)
        {
            CanWrite();
            Super = superClass;
            foreach (var item in superClass.methods)
            {
                var k = item.Key;
                var v = item.Value;
                AddMethod(k, v);
            }
        }

        public void AddMixin(ClassInternal flavour)
        {
            CanWrite();
            flavours[flavour.Name] = flavour;

            foreach (var flavourMeth in flavour.methods)
            {
                MixinMethod(flavourMeth.Key, flavourMeth.Value);
            }

            foreach (var flavourInitChain in flavour.InitChains)
            {
                if(!InitChains.Contains(flavourInitChain))
                {
                    InitChains.Add(flavourInitChain);
                }
            }
        }

        private void MixinMethod(string key, Value value)
        {
            if(methods.TryGetValue(key, out var existing))
            {
                //combine
                if (existing.type == ValueType.Closure)
                {
                    //make a combine
                    var temp = Value.Combined();
                    temp.val.asCombined.Add(existing.val.asClosure);
                    temp.val.asCombined.Add(value.val.asClosure);
                    existing = temp;
                }
                else
                {
                    existing.val.asCombined.Add(value.val.asClosure);
                }

                value = existing;
            }

            AddMethod(key, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Value GetMathOpClosure(OpCode opCode)
        {
            int opIndex = (int)opCode - ClassInternal.FirstMathOp;
            return mathOperators[opIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Value GetCompareOpClosure(OpCode opCode)
        {
            int opIndex = (int)opCode - ClassInternal.FirstCompOp;
            return compOperators[opIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void FinishCreation(InstanceInternal inst)
        {
            inst.Freeze();
        }
    }
}
