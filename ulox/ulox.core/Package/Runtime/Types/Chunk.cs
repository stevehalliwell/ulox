using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class Chunk
    {
        public const int InstructionStartingCapacity = 50;
        public const int ConstantStartingCapacity = 15;
        public const string DefaultChunkName = "unnamed_chunk";
        
        internal struct RunLengthLineNumber
        {
            public int startingInstruction;
            public int line;
        }

        private readonly List<Value> _constants = new List<Value>(ConstantStartingCapacity);
        private readonly List<RunLengthLineNumber> _runLengthLineNumbers = new List<RunLengthLineNumber>();
        private readonly Dictionary<byte, int> _labelIdToInstruction = new Dictionary<byte, int>();
        private int instructionCount = -1;

        public List<ByteCodePacket> Instructions { get; } = new List<ByteCodePacket>(InstructionStartingCapacity);
        public List<byte> ArgumentConstantIds { get; } = new List<byte>(5);
        public List<byte> ReturnConstantIds { get; } = new List<byte>(5);
        public IReadOnlyList<Value> Constants => _constants.AsReadOnly();
        public IReadOnlyDictionary<byte, int> Labels => _labelIdToInstruction;
        public string Name { get; set; }
        public string SourceName { get; }
        public FunctionType FunctionType { get; internal set; }
        public byte Arity => (byte)ArgumentConstantIds.Count;
        public byte ReturnCount => (byte)ReturnConstantIds.Count;
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
        public byte AddConstantAndWriteInstruction(Value val, int line)
        {
            var at = AddConstant(val);
            WritePacket(new ByteCodePacket(OpCode.PUSH_CONSTANT, at, 0, 0), line);
            return at;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WritePacket(ByteCodePacket packet, int line)
        {
            Instructions.Add(packet);
            AddLine(line);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte AddConstant(Value val)
        {
            var existingLox = ExistingSimpleConstant(val);
            if (existingLox != -1) return (byte)existingLox;

            if (_constants.Count >= byte.MaxValue)
                throw new UloxException($"Cannot have more than '{byte.MaxValue}' constants per chunk.");

            _constants.Add(val);
            return (byte)(_constants.Count - 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Value ReadConstant(byte index) => _constants[index];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int ExistingSimpleConstant(Value val)
        {
            switch (val.type)
            {
            case ValueType.Double:
                return _constants.FindIndex(x => x.type == val.type && val.val.asDouble == x.val.asDouble);
                
            case ValueType.String:
                return _constants.FindIndex(x => x.type == val.type && val.val.asString == x.val.asString);
            // none of those are going to be duplicated by the compiler anyway
            case ValueType.Bool:
            case ValueType.Null:
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

        internal void RemoveInstructionAt(int b)
        {
            Instructions.RemoveAt(b);
            AdjustLabelIndicies(b, -1);
            AdjustLineNumbers(b, -1);
        }

        internal void AdjustLabelIndicies(int byteChangedThresholde, int delta)
        {
            foreach (var item in _labelIdToInstruction.ToList())
            {
                if (item.Value < byteChangedThresholde) continue;
                _labelIdToInstruction[item.Key] = item.Value + delta;
            }
        }

        private void AdjustLineNumbers(int byteChangedThresholde, int delta)
        {
            for (var i = 0; i < _runLengthLineNumbers.Count; i++)
            {
                var item = _runLengthLineNumbers[i];
                if (item.startingInstruction < byteChangedThresholde) continue;
                _runLengthLineNumbers[i] = new RunLengthLineNumber()
                {
                    line = item.line,
                    startingInstruction = item.startingInstruction + delta
                };
            }
        }
    }
}
