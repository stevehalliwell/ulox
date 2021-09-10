using System;
using System.Collections.Generic;

namespace ULox
{
    public class Engine
    {
        public Engine(IContext executionContext)
        {
            Context = executionContext;
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

        public IContext Context { get; private set; }

        public void BindAllLibraries()
        {
            foreach (var libName in Context.LibraryNames)
            {
                Context.BindLibrary(libName);
            }
        }

        public void RunScript(string script)
        {
            var s = Context.CompileScript(script);
            Context.VM.Interpret(s.TopLevelChunk);
        }
    }
}
