namespace ULox
{
    public class ULoxScriptEnvironment
    {
        private readonly ByteCodeInterpreterEngine _engine;
        public ByteCodeInterpreterEngine SharedEngine => _engine;

        public ULoxScriptEnvironment(ByteCodeInterpreterEngine engine) => _engine = engine;

        public void CallFunction(Value value, int v) => _engine.VM.CallFunction(value, v);

        public Value FindFunctionWithArity(string name, int argCount) => _engine.VM.FindFunctionWithArity(name, argCount);

        public Value? GetGlobal(string v)
        {
            try
            {
                return _engine.VM.GetGlobal(v);
            }
            catch (System.Exception)
            {
            }
            return null;
        }

        public void Run(string script) => _engine.Run(script);

        public void SetGlobal(string v, Value value) => _engine.VM.SetGlobal(v, value);
    }
}
