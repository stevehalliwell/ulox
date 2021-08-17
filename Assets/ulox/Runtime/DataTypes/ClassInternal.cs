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
        private Dictionary<string, Value> methods = new Dictionary<string, Value>();
        public Value initialiser = Value.Null();
        public ClosureInternal initChainStartClosure;
        public int initChainStartLocation = -1;
        public Value[] mathOperators = new Value[LastMathOp - FirstMathOp + 1];
        public Value[] compOperators = new Value[LastCompOp - FirstCompOp + 1];
        public ClassInternal super;

        public Value GetMethod(string name) => methods[name];

        public bool TryGetMethod(string name, out Value method) => methods.TryGetValue(name, out method);

        public void AddMethod(string name, Value method)
        {
            methods[name] = method;
            if (name == ClassCompilette.InitMethodName)
            {
                initialiser = method;
            }
            var opIndex = MathOperatorMethodNames.IndexOf(name);
            if (opIndex != -1)
            {
                mathOperators[opIndex] = method;
            }
            else
            {
                opIndex = ComparisonOperatorMethodNames.IndexOf(name);
                if (opIndex != -1)
                {
                    compOperators[opIndex] = method;
                }
            }
        }

        internal void InheritFrom(ClassInternal superClass)
        {
            super = superClass;
            foreach (var item in superClass.methods)
            {
                var k = item.Key;
                var v = item.Value;
                AddMethod(k, v);
            }
        }
    }
}
