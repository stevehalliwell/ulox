namespace ULox
{
    public class VMLibrary : ILoxByteCodeLibrary
    {
        public void BindToEngine(ByteCodeInterpreterEngine engine)
        {
            engine.VM.SetGlobal("CreateEnvironment", Value.New(
                (vm, args) =>
                {
                    return Value.Object(new Environment());
                }));
            engine.VM.SetGlobal("SetEnvironment", Value.New(
                (vm, args) =>
                {
                    var asEnv = vm.GetArg(1).val.asObject as Environment;
                    vm.Environment = asEnv;
                    return Value.Null();
                }));
        }
    }

    //todo add tests
    public class DebugLibrary : ILoxByteCodeLibrary
    {
        public void BindToEngine(ByteCodeInterpreterEngine engine)
        {
            engine.VM.SetGlobal("GenerateStackDump", Value.New(
                (vm, args) =>
                {
                    return Value.New(vm.GenerateStackDump());
                }));
            engine.VM.SetGlobal("GenerateGlobalsDump", Value.New(
                (vm, args) =>
                {
                    return Value.New(vm.GenerateGlobalsDump());
                }));
        }
    }
}
