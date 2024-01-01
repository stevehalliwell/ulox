namespace ULox
{
    public sealed class OptimiserRemoveLabelsPass : CompiledScriptIterator, IOptimiserPass
    {
        private Optimiser _optimiser;

        public void Run(Optimiser optimiser, CompiledScript compiledScript)
        {
            _optimiser = optimiser;
            Iterate(compiledScript);
        }

        protected override void PreChunkInterate(CompiledScript compiledScript, Chunk chunk)
        {
        }

        protected override void ProcessPacket(ByteCodePacket packet)
        {
            switch (packet.OpCode)
            {
            case OpCode.LABEL:
                _optimiser.AddToRemove(CurrentChunk, CurrentInstructionIndex);
                break;
            }
        }

        protected override void PostChunkIterate(CompiledScript compiledScript, Chunk chunk)
        {
        }

        public void Reset()
        {
        }
    }
}
