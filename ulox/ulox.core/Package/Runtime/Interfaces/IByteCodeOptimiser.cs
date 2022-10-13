namespace ULox
{
    public interface IByteCodeOptimiser
    {
        bool Enabled { get; set; }

        void Optimise(CompiledScript compiledScript);
        void Reset();
    }
}