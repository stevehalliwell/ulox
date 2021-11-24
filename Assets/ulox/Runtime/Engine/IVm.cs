namespace ULox
{
    public interface IVm
    {
        IEngine Engine { get; }

        string GenerateGlobalsDump();
        string GenerateStackDump();
        Value GetGlobal(HashedString name);
        InterpreterResult Interpret(Chunk chunk);
        InterpreterResult PushCallFrameAndRun(Value func, int args);
        InterpreterResult Run(IProgram program);
        void SetGlobal(HashedString name, Value val);
        void SetEngine(IEngine engine);
        void CopyFrom(IVm otherVM);
        Value FindFunctionWithArity(HashedString name, int arity);
    }
}