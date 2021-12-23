using System;
using System.Collections.Generic;

namespace ULox
{
    public class Engine : IEngine
    {
        public IContext Context { get; private set; }
        public IScriptLocator ScriptLocator { get; private set; }
        public readonly Queue<string> _buildQueue = new Queue<string>();

        public Engine(
            IScriptLocator scriptLocator,
            IContext executionContext)
        {
            ScriptLocator = scriptLocator;
            Context = executionContext;
            Context.VM.SetEngine(this);
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
        {
            _buildQueue.Enqueue(ScriptLocator.Find(name));
        }
    }

    public static partial class EngineExt
    {
        public static void DeclareAllLibraries(
            this Engine engine,
            Action<string> logger,
            Func<VMBase> createVM)
        {
            engine.Context.DeclareLibrary(new CoreLibrary(logger));
            engine.Context.DeclareLibrary(new StandardClassesLibrary());
            engine.Context.DeclareLibrary(new AssertLibrary(createVM));
            engine.Context.DeclareLibrary(new DebugLibrary());
            engine.Context.DeclareLibrary(new VmLibrary(createVM));
        }

        public static void BindAllLibraries(this Engine engine)
        {
            foreach (var libName in engine.Context.LibraryNames)
            {
                engine.Context.BindLibrary(libName);
            }
        }
    }
}
