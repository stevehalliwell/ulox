namespace ULox
{
    public class CompiledScript
    {
        public Chunk TopLevelChunk;
        public int ScriptHash;

        public CompiledScript(Chunk topLevelChunk, int scriptHash)
        {
            TopLevelChunk = topLevelChunk;
            ScriptHash = scriptHash;
        }
    }
}
