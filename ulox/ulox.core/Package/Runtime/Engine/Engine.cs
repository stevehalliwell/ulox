using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class Engine
    {
        public Context Context { get; }
        private readonly BuildQueue _buildQueue = new BuildQueue();

        public Engine(Context executionContext)
        {
            Context = executionContext;
            Context.Vm.SetEngine(this);
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
            while (_buildQueue.HasItems)
            {
                var script = _buildQueue.Dequeue();
                var s = Context.CompileScript(script);
                Context.Vm.Interpret(s.TopLevelChunk);
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
