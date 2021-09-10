namespace ULox
{
    public interface IVM
    {
        string GenerateGlobalsDump();
        string GenerateStackDump();
        Value GetGlobal(string name);
        InterpreterResult Interpret(Chunk chunk);
        InterpreterResult PushCallFrameAndRun(Value func, int args);
        InterpreterResult Run(IProgram program);
        void SetGlobal(string name, Value val);
    }
}