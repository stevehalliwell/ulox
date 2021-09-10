namespace ULox
{
    public class ULoxScriptEnvironment
    {
        private readonly Engine _engine;
        public Engine SharedEngine => _engine;

        public ULoxScriptEnvironment(Engine engine) => _engine = engine;

        public void CallFunction(Value value, int v) => _engine.Context.VM.PushCallFrameAndRun(value, v);

        //public Value FindFunctionWithArity(string name, int argCount) => _engine.VM.FindFunctionWithArity(name, argCount);

        public Value? GetGlobal(string v)
        {
            try
            {
                return _engine.Context.VM.GetGlobal(v);
            }
            catch (System.Exception)
            {
            }
            return null;
        }

        public void Run(string script) => _engine.RunScript(script);

        public void SetGlobal(string v, Value value) => _engine.Context.VM.SetGlobal(v, value);
    }
}
