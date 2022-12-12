using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class Chunk
    {
        internal struct RunLengthLineNumber
        {
            public int startingInstruction;
            public int line;
        }

        private const string DefaultChunkName = "unnamed_chunk";
        private readonly List<Value> constants = new List<Value>();
        private readonly List<RunLengthLineNumber> _runLengthLineNumbers = new List<RunLengthLineNumber>();
        private readonly Dictionary<byte, int> _labelIdToInstruction = new Dictionary<byte, int>();
        private int instructionCount = -1;

        public List<byte> Instructions { get; private set; } = new List<byte>();
        public List<byte> ArgumentConstantIds { get; private set; } = new List<byte>();
        public IReadOnlyList<Value> Constants => constants.AsReadOnly();
        public IReadOnlyDictionary<byte, int> Labels => _labelIdToInstruction;
        public string Name { get; set; }
        public string SourceName { get; private set; }
        public FunctionType FunctionType { get; internal set; }
        public bool IsLocal => FunctionType == FunctionType.LocalFunction || FunctionType == FunctionType.LocalMethod;
        public bool IsPure => FunctionType == FunctionType.PureFunction;
        public int Arity => ArgumentConstantIds.Count;
        public int UpvalueCount { get; internal set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Chunk(string chunkName, string sourceName, FunctionType functionType)
        {
            Name = string.IsNullOrEmpty(chunkName) ? DefaultChunkName : chunkName;
            SourceName = sourceName;
            FunctionType = functionType;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteByte(byte b, int line)
        {
            Instructions.Add(b);
            AddLine(line);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte AddConstantAndWriteInstruction(Value val, int line)
        {
            Instructions.Add((byte)OpCode.CONSTANT);
            var at = AddConstant(val);
            Instructions.Add(at);
            AddLine(line);
            return at;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSimple(OpCode opCode, int line)
        {
            Instructions.Add((byte)opCode);
            AddLine(line);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte AddConstant(Value val)
        {
            var existingLox = ExistingSimpleConstant(val);
            if (existingLox != -1) return (byte)existingLox;

            if (constants.Count >= byte.MaxValue)
                throw new UloxException($"Cannot have more than '{byte.MaxValue}' constants per chunk.");

            constants.Add(val);
            return (byte)(constants.Count - 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Value ReadConstant(byte index) => constants[index];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int ExistingSimpleConstant(Value val)
        {
            switch (val.type)
            {
            case ValueType.Null:
                throw new UloxException("Attempted to add a null constant");
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
            case ValueType.UserType:
            case ValueType.Instance:
            case ValueType.BoundMethod:
            default:
                return -1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetLocationString(int line = -1)
        {
            var locationName = Name;

            var scriptOrgin = SourceName;
            if (!string.IsNullOrEmpty(scriptOrgin))
            {
                locationName += $"({scriptOrgin}{(line == -1 ? "" : $":{line}")})";
            }

            return locationName;
        }

        internal int GetLabelPosition(byte labelID)
        {
            return _labelIdToInstruction[labelID];
        }

        internal void AddLabel(byte id, int currentChunkInstructinCount)
        {
            _labelIdToInstruction[id] = currentChunkInstructinCount;
        }

        internal void AdjustLabelIndicies(int byteChangedThresholde, int delta)
        {
            foreach (var item in _labelIdToInstruction.ToList())
            {
                if (item.Value < byteChangedThresholde) continue;
                _labelIdToInstruction[item.Key] = item.Value + delta;
            }
        }
    }
}
