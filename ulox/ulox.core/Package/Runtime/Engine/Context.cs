namespace ULox
{
    public interface IULoxLibrary
    {
        Table GetBindings();
    }

    public sealed class Context
    {
        public Context(
            Program program,
            Vm vm,
            IPlatform platform)
        {
            Program = program;
            Vm = vm;
            Platform = platform;
        }

        public Program Program { get; }
        public Vm Vm { get; }
        public IPlatform Platform { get; }

        public void AddLibrary(IULoxLibrary lib)
        {
            var toAdd = lib.GetBindings();

            Vm.Globals.CopyFrom(toAdd);
        }

        public CompiledScript CompileScript(Script script)
        {
            var existing = Program.CompiledScripts.Find(x => x.ScriptHash == script.ScriptHash);
            if (existing != null)
                return existing;

            var res = Program.Compile(script);
            Vm.PrepareTypes(Program.TypeInfo);
            Vm.Clear();
            Vm.Interpret(res.TopLevelChunk);
            return res;
        }
    }
}
