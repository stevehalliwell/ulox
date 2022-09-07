using System.Collections.Generic;

namespace ULox
{
    public class Chunk
    {
        internal struct RunLengthLineNumber
        {
            public int startingInstruction;
            public int line;
        }

        private readonly List<Value> constants = new List<Value>();
        private readonly List<RunLengthLineNumber> _runLengthLineNumbers = new List<RunLengthLineNumber>();
        private int instructionCount = -1;

        public List<byte> Instructions { get; private set; } = new List<byte>();
        public List<byte> ArgumentConstantIds { get; private set; } = new List<byte>();
        public IReadOnlyList<Value> Constants => constants.AsReadOnly();
        public string Name { get; set; }
        public FunctionType FunctionType { get; internal set; }
        public bool IsLocal => FunctionType == FunctionType.LocalFunction || FunctionType == FunctionType.LocalMethod;
        public bool IsPure => FunctionType == FunctionType.PureFunction;
        public int Arity => ArgumentConstantIds.Count;
        public int UpvalueCount { get; internal set; }

        public Chunk(string name, FunctionType functionType)
        {
            Name = name;
            FunctionType = functionType;
        }

        public int GetLineForInstruction(int instructionNumber)
        {
            if (_runLengthLineNumbers.Count == 0) return -1;

            for (int i = 0; i < _runLengthLineNumbers.Count; i++)
            {
                if (instructionNumber < _runLengthLineNumbers[i].startingInstruction)
                    return _runLengthLineNumbers[i - 1].line;
            }

            return _runLengthLineNumbers[_runLengthLineNumbers.Count - 1].line;
        }

        public void WriteByte(byte b, int line)
        {
            Instructions.Add(b);
            AddLine(line);
        }

        public byte AddConstantAndWriteInstruction(Compiler compiler, Value val, int line)
        {
            Instructions.Add((byte)OpCode.CONSTANT);
            var at = AddConstant(compiler, val);
            Instructions.Add(at);
            AddLine(line);
            return at;
        }

        public void WriteSimple(OpCode opCode, int line)
        {
            Instructions.Add((byte)opCode);
            AddLine(line);
        }

        public byte AddConstant(Compiler compiler, Value val)
        {
            var existingLox = ExistingSimpleConstant(compiler, val);
            if (existingLox != -1) return (byte)existingLox;

            if (constants.Count >= byte.MaxValue)
                compiler.ThrowCompilerException($"Cannot have more than '{byte.MaxValue}' constants per chunk.");

            constants.Add(val);
            return (byte)(constants.Count - 1);
        }

        public Value ReadConstant(byte index) => constants[index];

        private int ExistingSimpleConstant(Compiler compiler, Value val)
        {
            switch (val.type)
            {
            case ValueType.Null:
                compiler.ThrowCompilerException("Attempted to add a null constant");
                return -1;
            case ValueType.Double:
                return constants.FindIndex(x => x.type == val.type && val.val.asDouble == x.val.asDouble);

            case ValueType.Bool:
                return constants.FindIndex(x => x.type == val.type && val.val.asBool == x.val.asBool);

            case ValueType.String:
                return constants.FindIndex(x => x.type == val.type && val.val.asString == x.val.asString);
            // none of those are going to be duplicated by the compiler anyway
            case ValueType.Object:
            case ValueType.Chunk:
            case ValueType.NativeFunction:
            case ValueType.Closure:
            case ValueType.Upvalue:
            case ValueType.Class:
            case ValueType.Instance:
            case ValueType.BoundMethod:
            default:
                return -1;
            }
        }

        private void AddLine(int line)
        {
            instructionCount++;

            if (_runLengthLineNumbers.Count == 0)
            {
                _runLengthLineNumbers.Add(new RunLengthLineNumber()
                {
                    line = line,
                    startingInstruction = instructionCount
                });
                return;
            }

            if (_runLengthLineNumbers[_runLengthLineNumbers.Count - 1].line != line)
            {
                _runLengthLineNumbers.Add(new RunLengthLineNumber()
                {
                    line = line,
                    startingInstruction = instructionCount
                });
            }
        }
    }
}
