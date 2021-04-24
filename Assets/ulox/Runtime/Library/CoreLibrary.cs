namespace ULox
{
    public class CoreLibrary : ILoxByteCodeLibrary
    {
        private System.Action<string> _printer;

        public CoreLibrary(System.Action<string> printer) { _printer = printer; }

        public void BindToEngine(ByteCodeInterpreterEngine engine)
        {
            engine.VM.SetGlobal("print", Value.New(
                (vm, args) =>
                {
                    _printer?.Invoke(vm.GetArg(1).ToString());
                    return Value.Null();
                }));
        }
    }
}
