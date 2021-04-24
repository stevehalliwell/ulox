using System;

namespace ULox
{
    public class ULoxScriptEnvironment
    {
        private ByteCodeInterpreterEngine _engine;

        public ByteCodeInterpreterEngine SharedEngine => _engine;
        public Environment LocalEnvironemnt { get; private set; } = new Environment();

        public ULoxScriptEnvironment(ByteCodeInterpreterEngine engine)
        {
            _engine = engine;
        }

        public void SetGlobal(string v, Value value) => _engine.VM.SetGlobal(v, value);

        public void RunLocal(string script)
        {
            var prevEnv = _engine.VM.Environment;
            _engine.VM.Environment = LocalEnvironemnt;
            _engine.Run(script);
            _engine.VM.Environment = prevEnv;
        }

        public void Run(string script)
        {
            _engine.Run(script);
        }

        public void CallFunction(Value value, int v)
        {
            var prevEnv = _engine.VM.Environment;
            _engine.VM.Environment = LocalEnvironemnt;
            _engine.VM.CallFunction(value, v);
            _engine.VM.Environment = prevEnv;
        }

        public Value GetGlobal(string v) => _engine.VM.GetGlobal(v);

        public Value FindFunctionWithArity(string name, int arity)
        {
            if (LocalEnvironemnt.TryGetValue(name, out var val))
            {
                if (val.type == Value.Type.Closure &&
                    val.val.asClosure.chunk.Arity == arity)
                {
                    return val;
                }
            }

            var globalVal = GetGlobal(name);

            if (globalVal.type == Value.Type.Closure &&
                    globalVal.val.asClosure.chunk.Arity == arity)
            {
                return globalVal;
            }

            return Value.Null();
        }
    }
}
