using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ULox
{
    public readonly struct Label : System.IEquatable<Label>
    {
        public static readonly Label Default = new(ushort.MaxValue);

        public Label(ushort id)
        {
            this.id = id;
        }
        public readonly ushort id;

        public override bool Equals(object obj)
        {
            return obj is Label label && Equals(label);
        }

        public bool Equals(Label other)
        {
            return id == other.id;
        }

        public override int GetHashCode()
        {
            return 1877310944 + id.GetHashCode();
        }

        public override string ToString()
        {
            return id.ToString();
        }

        public static bool operator ==(Label left, Label right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Label left, Label right)
        {
            return !(left == right);
        }

        static public (byte higher, byte lower) ToBytePair(ushort id)
        {
            return ((byte)(id >> 8), (byte)(id & 0xff));
        }

        static public Label FromBytePair(byte higher, byte lower)
        {
            return new Label((ushort)(higher << 8 | lower));
        }
    }

    //TODO split this up into pure data and operations on it
    public sealed class Chunk
    {
        public const int InstructionStartingCapacity = 50;
        public const int ConstantStartingCapacity = 15;
        public const string InternalLabelPrefix = "INTERNAL_";

        public struct RunLengthLineNumber
        {
            public int startingInstruction;
            public int line;

            public override string ToString()
            {
                return $"line {line} at instruction {startingInstruction}";
            }
        }

        public readonly List<Value> Constants = new(ConstantStartingCapacity);
        public readonly List<RunLengthLineNumber> RunLengthLineNumbers = new();
        public readonly Dictionary<Label, int> Labels = new();
        public readonly Dictionary<Label, HashedString> LabelNames = new();
        public readonly List<ByteCodePacket> Instructions = new(InstructionStartingCapacity);
        public readonly List<byte> ArgumentConstantIds = new(5);
        public readonly List<byte> ReturnConstantIds = new(5);

        public readonly string ChunkName;
        public readonly string SourceName;
        public readonly string ContainingChunkChainName;
        public int UpvalueCount;
        public int instructionCount = -1;

        public string FullName => $"{SourceName}:{ContainingChunkChainName}.{ChunkName}";

        public Chunk(string chunkName, string sourceName, string containingChunkChainName)
        {
            ChunkName = chunkName;
            SourceName = sourceName;
            ContainingChunkChainName = containingChunkChainName;
        }

        public int GetLineForInstruction(int instructionNumber)
        {
            if (RunLengthLineNumbers.Count == 0) return -1;

            for (int i = 0; i < RunLengthLineNumbers.Count; i++)
            {
                if (instructionNumber < RunLengthLineNumbers[i].startingInstruction)
                {
                    var previous = i - 1;
                    if (previous < 0) return 0;
                    return RunLengthLineNumbers[i - 1].line;
                }
            }

            return RunLengthLineNumbers[RunLengthLineNumbers.Count - 1].line;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WritePacket(ByteCodePacket packet, int line)
        {
            Instructions.Add(packet);

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte AddConstant(Value val)
        {
            var existingLox = -1;

            switch (val.type)
            {
            case ValueType.Double:
                existingLox = Constants.FindIndex(x => x.type == val.type && val.val.asDouble == x.val.asDouble);
                break;
            case ValueType.String:
                existingLox = Constants.FindIndex(x => x.type == val.type && val.val.asString == x.val.asString);
                break;
            }
            if (existingLox != -1) return (byte)existingLox;

            if (Constants.Count >= byte.MaxValue)
                throw new UloxException($"Cannot have more than '{byte.MaxValue}' constants per chunk, when attempting to add '{val}' in chunk '{this}'");

            Constants.Add(val);
            return (byte)(Constants.Count - 1);
        }

        public Value ReadConstant(byte index) => Constants[index];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetLocationString(int line = -1)
        {
            var locationName = ChunkName;

            var scriptOrgin = SourceName;
            if (!string.IsNullOrEmpty(scriptOrgin))
            {
                locationName += $"({scriptOrgin}{(line == -1 ? "" : $":{line}")})";
            }

            return locationName;
        }

        //TODO: the only actual uses of this are goto, goto_if, and init chains. So these don't need to be bytes
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort GetLabelPosition(Label labelID)
        {
            return (ushort)(Labels[labelID] + 1);
        }

        public void InsertInstructionsAt(int at, IReadOnlyList<ByteCodePacket> toMove)
        {
            Instructions.InsertRange(at, toMove);
            AdjustLabelIndicies(at, toMove.Count);
            for (int i = 0; i < toMove.Count; i++)
            {
                AdjustLineNumbers(at + i, 1);
            }
        }

        public void AdjustLabelIndicies(int byteChangedThreshold, int delta)
        {
            foreach (var item in Labels.ToList())
            {
                if (item.Value < byteChangedThreshold) continue;
                Labels[item.Key] = item.Value + delta;
            }
        }

        public void AdjustLineNumbers(int byteChangedThreshold, int delta)
        {
            for (var i = 0; i < RunLengthLineNumbers.Count; i++)
            {
                var item = RunLengthLineNumbers[i];
                if (item.startingInstruction < byteChangedThreshold) continue;
                RunLengthLineNumbers[i] = new RunLengthLineNumber()
                {
                    line = item.line,
                    startingInstruction = item.startingInstruction + delta
                };
            }
        }

        public override string ToString()
        {
            return $"{FullName} ({Instructions.Count} instructions)";
        }

        public bool IsInternalLabel(Label key)
        {
            return LabelNames[key].String.StartsWith(InternalLabelPrefix);
        }

        public Label CreateLabel(HashedString hashedString)
        {
            return AddLabel(hashedString, -1);
        }

        public Label AddLabel(HashedString labelName, int currentChunkInstructinCount)
        {
            foreach (var item in LabelNames)
            {
                if (item.Value == labelName)
                {
                    return item.Key;
                }
            }
            ushort nextLabelId = 0;
            if (Labels.Count != 0)
            {
                var maxCurLabelId = Labels.Keys.Max(x => x.id);
                if (maxCurLabelId >= Label.Default.id)
                    throw new UloxException($"Cannot have more than '{Label.Default.id}' labels, when attempting to add '{labelName.String}' in chunk '{this}'");
                nextLabelId = (ushort)(maxCurLabelId + 1);
            }
            var label = new Label(nextLabelId);
            AddLabel(label, currentChunkInstructinCount);
            LabelNames.Add(label, labelName);
            return label;
        }

        public void AddLabel(Label id, int currentChunkInstructinCount)
        {
            Labels[id] = currentChunkInstructinCount;
        }

        public void RemoveLabel(Label label)
        {
            Labels.Remove(label);
            LabelNames.Remove(label);
        }

        public Chunk DeepClone()
        {
            var newChunk = new Chunk(ChunkName, SourceName, ContainingChunkChainName);

            foreach (var constant in Constants)
            {
                newChunk.Constants.Add(constant);
            }

            foreach (var instruction in Instructions)
            {
                newChunk.Instructions.Add(instruction);
            }

            foreach (var label in Labels)
            {
                newChunk.Labels.Add(label.Key, label.Value);
            }

            foreach (var line in RunLengthLineNumbers)
            {
                newChunk.RunLengthLineNumbers.Add(line);
            }

            foreach (var arg in ArgumentConstantIds)
            {
                newChunk.ArgumentConstantIds.Add(arg);
            }

            foreach (var ret in ReturnConstantIds)
            {
                newChunk.ReturnConstantIds.Add(ret);
            }

            return newChunk;
        }
    }
}
