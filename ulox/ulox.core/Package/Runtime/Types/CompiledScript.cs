namespace ULox
{
    public sealed class CompiledScript
    {
        public Chunk TopLevelChunk { get; private set; }
        public int ScriptHash { get; private set; }

        public CompiledScript(Chunk topLevelChunk, int scriptHash)
        {
            TopLevelChunk = topLevelChunk;
            ScriptHash = scriptHash;
        }
    }
}
