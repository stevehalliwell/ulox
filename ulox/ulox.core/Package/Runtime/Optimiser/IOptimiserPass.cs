namespace ULox
{
    public interface IOptimiserPass
    {
        void Reset();
        void Run(Optimiser optimiser, CompiledScript compiledScript);
    }
}
