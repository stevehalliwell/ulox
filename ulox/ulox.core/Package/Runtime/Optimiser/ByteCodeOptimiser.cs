using System.Collections.Generic;
using System.Linq;

namespace ULox
{
    public sealed class ByteCodeOptimiser : CompiledScriptIterator
    {
        public bool Enabled { get; set; } = false;
        private List<(Chunk chunk, int inst)> _toRemove = new List<(Chunk, int)>();
        private readonly List<(Chunk chunk, int from, byte label)> _labelUsage = new List<(Chunk, int, byte)>();
        private OpCode _prevOoCode;
        private int _deadCodeStart = -1;

        public void Optimise(CompiledScript compiledScript)
        {
            if (!Enabled)
                return;

            Iterate(compiledScript);

            MarkNoJumpGotoLabelAsDead(compiledScript);
            MarkUnsedLabelsAsDead(compiledScript);
            RemoveMarkedInstructions();
        }

        private void MarkNoJumpGotoLabelAsDead(CompiledScript compiledScript)
        {
            foreach (var (chunk, from, label) in _labelUsage)
            {
                var labelLoc = chunk.Labels[label];

                if (from - 1 >= labelLoc)
                    continue;

                var found = false;
                for (int i = from + 1; i < labelLoc; i++)
                {
                    if (!_toRemove.Any(d => d.chunk == chunk && d.inst == i))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    _toRemove.Add((chunk, from));
                }
            }
        }

        private void MarkUnsedLabelsAsDead(CompiledScript compiledScript)
        {
            foreach (var chunk in compiledScript.AllChunks)
            {
                foreach (var label in chunk.Labels)
                {
                    var matches = _labelUsage.Where(x => x.chunk == chunk && x.label == label.Key);
                    var used = matches.Any(x => !_toRemove.Any(y => y.chunk == chunk && y.inst == x.from));
                    if (!used)
                    {
                        _toRemove.Add((chunk, label.Value));
                    }
                }
            }
        }

        private void RemoveMarkedInstructions()
        {
            _toRemove.Sort((x, y) => x.inst.CompareTo(y.inst));
            _toRemove = _toRemove.Distinct().ToList();

            for (int i = _toRemove.Count - 1; i >= 0; i--)
            {
                var (chunk, b) = _toRemove[i];
                chunk.RemoveInstructionAt(b);
            }
        }

        public void Reset()
        {
            _toRemove.Clear();
            _deadCodeStart = -1;
        }

        protected override void PostChunkIterate(CompiledScript compiledScript, Chunk chunk)
        {
        }

        protected override void PreChunkInterate(CompiledScript compiledScript, Chunk chunk)
        {
        }

        private void AddLabelUsage(byte labelId)
        {
            _labelUsage.Add((CurrentChunk, CurrentInstructionIndex, labelId));
        }

        protected override void ProcessPacket(ByteCodePacket packet)
        {
            var opCode = packet.OpCode;
            if (_deadCodeStart == -1
                && _prevOoCode == OpCode.GOTO
                && opCode != OpCode.LABEL)
            {
                _deadCodeStart = CurrentInstructionIndex;
            }

            if (opCode == OpCode.LABEL
                && _deadCodeStart != -1)
            {
                _toRemove.AddRange(Enumerable.Range(_deadCodeStart, CurrentInstructionIndex - _deadCodeStart).Select(b => (CurrentChunk, b)));
                _deadCodeStart = -1;
            }
            _prevOoCode = opCode;

            switch (packet.OpCode)
            {
            case OpCode.TYPE:
                AddLabelUsage(packet.typeDetails.initLabelId);
                break;
            case OpCode.TEST:
                if (packet.testOpDetails.TestOpType == TestOpType.TestFixtureBodyInstruction)
                    AddLabelUsage(packet.testOpDetails.b1);
                else if (packet.testOpDetails.TestOpType == TestOpType.TestCase)
                    AddLabelUsage(packet.testOpDetails.b2);
                break;
            case OpCode.GOTO:
            case OpCode.GOTO_IF_FALSE:
                ProcessGoto(packet);
                break;
            }
        }

        private void ProcessGoto(ByteCodePacket packet)
        {
            var endingLocation = CurrentChunk.Labels[packet.b1];
            if (endingLocation != CurrentInstructionIndex + 1)
                AddLabelUsage(packet.b1);
            else
                _toRemove.Add((CurrentChunk, CurrentInstructionIndex));
        }
    }
}
