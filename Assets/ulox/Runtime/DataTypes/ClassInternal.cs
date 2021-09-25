using System.Collections.Generic;

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

        public string name;
        private readonly Dictionary<string, Value> methods = new Dictionary<string, Value>();
        private readonly Dictionary<string, ClassInternal> flavours = new Dictionary<string, ClassInternal>();
        public Value initialiser = Value.Null();
        public readonly List<(ClosureInternal, int)> initChains = new List<(ClosureInternal, int)>();
        public readonly Value[] mathOperators = new Value[LastMathOp - FirstMathOp + 1];
        public readonly Value[] compOperators = new Value[LastCompOp - FirstCompOp + 1];
        public ClassInternal super;

        public Value GetMethod(string name) => methods[name];

        public bool TryGetMethod(string name, out Value method) => methods.TryGetValue(name, out method);

        public void AddMethod(string key, Value method)
        {
            methods[key] = method;
            if (key == ClassCompilette.InitMethodName)
            {
                initialiser = method;
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

        internal void AddInitChain(ClosureInternal closure, ushort initChainStartOp)
        {
            initChains.Add((closure, initChainStartOp));
        }

        public void InheritFrom(ClassInternal superClass)
        {
            super = superClass;
            foreach (var item in superClass.methods)
            {
                var k = item.Key;
                var v = item.Value;
                AddMethod(k, v);
            }
        }

        public void AddMixin(ClassInternal flavour)
        {
            flavours[flavour.name] = flavour;

            foreach (var flavourMeth in flavour.methods)
            {
                MixinMethod(flavourMeth.Key, flavourMeth.Value);
            }

            foreach (var flavourInitChain in flavour.initChains)
            {
                if(!initChains.Contains(flavourInitChain))
                {
                    initChains.Add(flavourInitChain);
                }
            }
        }

        public void MixinMethod(string key, Value value)
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
    }
}
