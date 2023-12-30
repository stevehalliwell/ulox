namespace ULox
{
    public sealed class ClosureInternal
    {
        public Chunk chunk;
        public Value[] upvalues;
        public override string ToString()
        {
            return $"<closure {chunk.ChunkName} upvals:{upvalues.Length}>";
        }
    }
}
