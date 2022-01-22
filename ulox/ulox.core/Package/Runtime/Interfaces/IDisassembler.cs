namespace ULox
{
    public interface IDisassembler
    {
        void DoChunk(Chunk chunk);
        string GetString();
    }
}