using System.Collections.Generic;

namespace ULox
{
    public class Chunk
    {
        public List<byte> instructions = new List<byte>();
        private List<Value> constants = new List<Value>();
        public IReadOnlyList<Value> Constants => constants.AsReadOnly();
        public List<RunLengthLineNumber> runLengthLineNumbers = new List<RunLengthLineNumber>();
        public int instructionCount = -1;
        public string Name { get; set; }
        public int Arity { get; set; }
        public int UpvalueCount { get; internal set; }

        public void AddLine(int line)
        {
            instructionCount++;

            if (runLengthLineNumbers.Count == 0)
            {
                runLengthLineNumbers.Add(new RunLengthLineNumber()
                {
                    line = line,
                    startingInstruction = instructionCount
                });
                return;
            }

            if (runLengthLineNumbers[runLengthLineNumbers.Count-1].line != line)
            {
                runLengthLineNumbers.Add(new RunLengthLineNumber()
                {
                    line = line,
                    startingInstruction = instructionCount
                });
            }
        }

        public int GetLineForInstruction(int instructionNumber)
        {
            if (runLengthLineNumbers.Count == 0) return -1;

            for (int i = 0; i < runLengthLineNumbers.Count; i++)
            {
                if (instructionNumber < runLengthLineNumbers[i].startingInstruction)
                    return runLengthLineNumbers[i-1].line;
            }

            return runLengthLineNumbers[runLengthLineNumbers.Count-1].line;
    }

        public Chunk(string name)
        {
            Name = name;
        }

        public void WriteByte(byte b, int line)
        {
            instructions.Add(b);
            AddLine(line);
        }

        public byte AddConstantAndWriteInstruction(Value val, int line)
        {
            instructions.Add((byte)OpCode.CONSTANT);
            var at = AddConstant(val);
            instructions.Add(at);
            AddLine(line);
            return at;
        }

        public void WriteSimple(OpCode opCode, int line)
        {
            instructions.Add((byte)opCode);
            AddLine(line);
        }

        public byte AddConstant(Value val)
        {
            var existingLox = ExistingSimpleConstant(val);
            if (existingLox != -1) return (byte)existingLox;

            if (constants.Count >= byte.MaxValue)
                throw new CompilerException($"Cannot have more than '{byte.MaxValue}' constants per chunk.");

            constants.Add(val);
            return (byte) (constants.Count - 1);
        }

        private int ExistingSimpleConstant(Value val)
        {
            switch (val.type)
            {
            case Value.Type.Null:
                throw new CompilerException("Attempted to add a null constant");
            case Value.Type.Double:
                return constants.FindIndex(x => x.type == val.type && val.val.asDouble == x.val.asDouble);
            case Value.Type.Bool:
                return constants.FindIndex(x => x.type == val.type && val.val.asBool == x.val.asBool);
            case Value.Type.String:
                return constants.FindIndex(x => x.type == val.type && val.val.asString == x.val.asString);
            // none of those are going to be duplicated by the compiler anyway
            case Value.Type.Object:
            case Value.Type.Chunk:
            case Value.Type.NativeFunction:
            case Value.Type.Closure:
            case Value.Type.Upvalue:
            case Value.Type.Class:
            case Value.Type.Instance:
            case Value.Type.BoundMethod:
            default:
                return -1;
            }
        }

        public Value ReadConstant(byte index) => constants[index];
    }

    public struct RunLengthLineNumber
    {
        public int startingInstruction;
        public int line;
    }
}
