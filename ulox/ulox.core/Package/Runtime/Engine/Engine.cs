using System.Runtime.CompilerServices;

namespace ULox
{
    //TODO: feelslike his could just be the context...
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
                Context.CompileScript(script);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LocateAndQueue(string filePath)
        {
            var script = new Script(filePath, Context.Platform.LoadFile(filePath));
            _buildQueue.Enqueue(script);
        }
    }
}
