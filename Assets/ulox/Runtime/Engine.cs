using System;
using System.Collections.Generic;

namespace ULox
{
    public interface IEngine
    {
        IContext Context { get; }
        IScriptLocator ScriptLocator { get; }

        void LocateAndQueue(string name);
    }

    public class Engine : IEngine
    {
        public IContext Context { get; private set; }
        public IScriptLocator ScriptLocator { get; private set; }
        public Builder Builder { get;private set; }
        public Queue<string> _buildQueue = new Queue<string>();

        public Engine(
            IScriptLocator scriptLocator,
            Builder builder,
            IContext executionContext)
        {
            ScriptLocator = scriptLocator;
            Builder = builder;
            Context = executionContext;
            Builder.SetEngine(this);
        }

        public void DeclareAllLibraries(
            Action<string> logger,
            List<UnityEngine.GameObject> availablePrefabs,
            Action<string> outputText,
            Func<VMBase> createVM)
        { 

            Context.DeclareLibrary(new CoreLibrary(logger));
            Context.DeclareLibrary(new StandardClassesLibrary());
            Context.DeclareLibrary(new UnityLibrary(availablePrefabs, outputText));
            Context.DeclareLibrary(new AssertLibrary(createVM));
            Context.DeclareLibrary(new DebugLibrary());
            Context.DeclareLibrary(new VMLibrary(createVM));
        }

        public void BindAllLibraries()
        {
            foreach (var libName in Context.LibraryNames)
            {
                Context.BindLibrary(libName);
            }
        }

        public void RunScript(string script)
        {
            _buildQueue.Enqueue(script);
            BuildAndRun();
        }

        public void BuildAndRun()
        {
            while(_buildQueue.Count > 0)
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

        public void LocateAndRun(string name)
        {
            RunScript(ScriptLocator.Find(name));
        }
    }
}
