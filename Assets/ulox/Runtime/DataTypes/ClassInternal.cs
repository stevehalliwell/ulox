using System.Collections.Generic;

namespace ULox
{
    public class ClassInternal : InstanceInternal
    {
        public const int FirstMathOp = (int)OpCode.ADD;
        public const int LastMathOp = (int)OpCode.MODULUS;
        public readonly static List<string> OperatorMethodNames = new List<string>()
        {
            "_add",
            "_sub",
            "_mul",
            "_div",
            "_mod",
        };

        public string name;
        private Dictionary<string, Value> methods = new Dictionary<string, Value>();
        public Value initialiser = Value.Null();
        public ClosureInternal initChainStartClosure;
        public int initChainStartLocation = -1;
        public Value[] operators = new Value[LastMathOp-FirstMathOp];

        public Value GetMethod(string name) => methods[name];
        public bool TryGetMethod(string name, out Value method) => methods.TryGetValue(name, out method);
        public void AddMethod(string name, Value method)
        {
            methods[name] = method;
            if (name == ClassCompilette.InitMethodName)
            {
                initialiser = method;
            }
            var opIndex = OperatorMethodNames.IndexOf(name);
            if (opIndex != -1)
            {
                operators[opIndex] = method;
            }
        }

        internal void InheritFrom(ClassInternal superClass)
        {
            foreach (var item in superClass.methods)
            {
                var k = item.Key;
                var v = item.Value;
                AddMethod(k, v);
            }
        }
    }
}
