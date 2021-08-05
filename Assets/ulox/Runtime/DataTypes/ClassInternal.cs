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
        public Dictionary<string, Value> methods = new Dictionary<string, Value>();
        public Value initialiser = Value.Null();
        public ClosureInternal initChainStartClosure;
        public int initChainStartLocation = -1;
        public Value[] operators = new Value[LastMathOp-FirstMathOp];
    }
}
