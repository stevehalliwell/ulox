using System;
using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class Engine
    {
        public Context Context { get; }
        private readonly BuildQueue _buildQueue = new();

        public Engine(Context executionContext)
        {
            Context = executionContext;
            Context.Vm.Engine = this;
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
                var s = Context.CompileScript(script, (s) =>
                {
                    Context.Vm.PrepareTypes(Context.Program.TypeInfo);
                    Context.Vm.Interpret(s.TopLevelChunk);
                });
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LocateAndQueue(string filePath)
        {
            var script = new Script(filePath, Context.Platform.LoadFile(filePath));
            _buildQueue.Enqueue(script);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Engine CreateDefault()
        {
            var context = new Context(new Program(), new Vm(), new DirectoryLimitedPlatform(new(Environment.CurrentDirectory)));
            var engine = new Engine(context);
            engine.Context.AddLibrary(new PrintLibrary(x => context.Log(x)));
            return engine;
        }
    }
}
