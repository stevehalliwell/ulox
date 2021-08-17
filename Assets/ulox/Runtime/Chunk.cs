using System.Collections.Generic;

namespace ULox
{
    public class Chunk
    {
        private readonly List<Value> constants = new List<Value>();
        private int instructionCount = -1;

        public List<byte> Instructions { get; private set; } = new List<byte>();
        public IReadOnlyList<Value> Constants => constants.AsReadOnly();
        public List<RunLengthLineNumber> RunLengthLineNumbers { get; private set; } = new List<RunLengthLineNumber>();
        public string Name { get; set; }
        public int Arity { get; set; }
        public int UpvalueCount { get; internal set; }

        public Chunk(string name)
        {
            Name = name;
        }

        public int GetLineForInstruction(int instructionNumber)
        {
            if (RunLengthLineNumbers.Count == 0) return -1;

            for (int i = 0; i < RunLengthLineNumbers.Count; i++)
            {
                if (instructionNumber < RunLengthLineNumbers[i].startingInstruction)
                    return RunLengthLineNumbers[i - 1].line;
            }

            return RunLengthLineNumbers[RunLengthLineNumbers.Count - 1].line;
        }

        public void WriteByte(byte b, int line)
        {
            Instructions.Add(b);
            AddLine(line);
        }

        public byte AddConstantAndWriteInstruction(Value val, int line)
        {
            Instructions.Add((byte)OpCode.CONSTANT);
            var at = AddConstant(val);
            Instructions.Add(at);
            AddLine(line);
            return at;
        }

        public void WriteSimple(OpCode opCode, int line)
        {
            Instructions.Add((byte)opCode);
            AddLine(line);
        }

        public byte AddConstant(Value val)
        {
            var existingLox = ExistingSimpleConstant(val);
            if (existingLox != -1) return (byte)existingLox;

            if (constants.Count >= byte.MaxValue)
                throw new CompilerException($"Cannot have more than '{byte.MaxValue}' constants per chunk.");

            constants.Add(val);
            return (byte)(constants.Count - 1);
        }

        public Value ReadConstant(byte index) => constants[index];

        private int ExistingSimpleConstant(Value val)
        {
            switch (val.type)
            {
                case ValueType.Null:
                    throw new CompilerException("Attempted to add a null constant");
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

            if (RunLengthLineNumbers.Count == 0)
            {
                RunLengthLineNumbers.Add(new RunLengthLineNumber()
                {
                    line = line,
                    startingInstruction = instructionCount
                });
                return;
            }

            if (RunLengthLineNumbers[RunLengthLineNumbers.Count - 1].line != line)
            {
                RunLengthLineNumbers.Add(new RunLengthLineNumber()
                {
                    line = line,
                    startingInstruction = instructionCount
                });
            }
        }
    }
}
