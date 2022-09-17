using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class Engine : IEngine
    {
        public IContext Context { get; private set; }
        public readonly Queue<Script> _buildQueue = new Queue<Script>();

        public Engine(IContext executionContext)
        {
            Context = executionContext;
            Context.VM.SetEngine(this);
            Context.AddLibrary(new StdLibrary());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunScript(Script script)
        {
            _buildQueue.Enqueue(script);
            BuildAndRun();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BuildAndRun()
        {
            while (_buildQueue.Count > 0)
            {
                var script = _buildQueue.Dequeue();
                var s = Context.CompileScript(script);
                Context.VM.Interpret(s.TopLevelChunk);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LocateAndQueue(string name)
            => _buildQueue.Enqueue(Context.ScriptLocator.Find(name));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Engine CreateDefault()
        {
            var context = new Context(new LocalFileScriptLocator(), new Program(), new Vm());
            var engine = new Engine(context);
            engine.Context.AddLibrary(new PrintLibrary(x => context.Log(x)));
            return engine;
        }
    }
}
