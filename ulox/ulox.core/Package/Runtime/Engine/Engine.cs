using System.Collections.Generic;

namespace ULox
{
    public class Engine : IEngine
    {
        public IContext Context { get; private set; }
        public readonly Queue<string> _buildQueue = new Queue<string>();

        public Engine(IContext executionContext)
        {
            Context = executionContext;
            Context.VM.SetEngine(this);
            Context.AddLibrary(new AssertLibrary());
            Context.AddLibrary(new SerialiseLibrary());
            Context.AddLibrary(new DebugLibrary());
            Context.AddLibrary(new DiLibrary());
            Context.AddLibrary(new FreezeLibrary());
        }

        public void RunScript(string script)
        {
            _buildQueue.Enqueue(script);
            BuildAndRun();
        }

        public void BuildAndRun()
        {
            while (_buildQueue.Count > 0)
            {
                var script = _buildQueue.Dequeue();
                var s = Context.CompileScript(script);
                Context.VM.Interpret(s.TopLevelChunk);
            }
        }

        public void LocateAndQueue(string name)
            => _buildQueue.Enqueue(Context.ScriptLocator.Find(name));

        public static Engine CreateDefault()
        {
            var context = new Context(new ScriptLocator(), new Program(), new Vm());
            var engine = new Engine(context);
            engine.Context.AddLibrary(new CoreLibrary(x => context.Log(x)));
            return engine;
        }
    }
}
