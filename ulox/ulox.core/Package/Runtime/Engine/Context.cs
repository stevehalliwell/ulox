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
        public bool ReinterpretOnEachCompile { get; set; } = false;

        public void AddLibrary(IULoxLibrary lib)
        {
            var toAdd = lib.GetBindings();

            Vm.Globals.CopyFrom(toAdd);
        }

        public CompiledScript CompileScript(Script script)
        {
            var compScript = Program.CompiledScripts.Find(x => x.ScriptHash == script.ScriptHash);
            if (compScript != null)
            {
                if(ReinterpretOnEachCompile)
                {
                    Vm.PrepareTypes(Program.TypeInfo);
                    Vm.Interpret(compScript.TopLevelChunk);
                }
                return compScript;
            }
            var res = Program.Compile(script);
            Vm.PrepareTypes(Program.TypeInfo);
            Vm.Interpret(res.TopLevelChunk);
            return res;
        }
    }
}
