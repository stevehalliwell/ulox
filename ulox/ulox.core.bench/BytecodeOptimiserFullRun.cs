using System;
using System.IO;
using BenchmarkDotNet.Attributes;

namespace ULox.Core.Bench
{
    [MemoryDiagnoser]
    [WarmupCount(3)]
    [IterationCount(5)]
    public class BytecodeOptimiserFullRun
    {
        [Benchmark]
        public void OptimisationsOff()
        {
            Run(engine => engine.Context.Program.Optimiser.Enabled = false);
        }

        [Benchmark]
        public void OptimisationsOn()
        {
            Run(engine => engine.Context.Program.Optimiser.Enabled = true);
        }

        private static void Run(Action<Engine> engineAction)
        {
            var engine = Program.CreateEngine();
            engineAction(engine);
            engine.RunScript(Vec2Variants.Tuple);

            engine = Program.CreateEngine();
            engineAction(engine);
            engine.RunScript(Vec2Variants.Type);

            RunScriptsInFolder(engineAction, Program.TryItOutFolderName);
            RunScriptsInFolder(engineAction, Program.NoFailFolderName);
        }

        private static void RunScriptsInFolder(Action<Engine> engineAction, string folderName)
        {
            foreach (var file in Program.GetScriptsInFolder(folderName))
            {
                var engine = Program.CreateEngine();
                engineAction(engine);
                engine.RunScript(new Script(file, File.ReadAllText(file)));
            }
        }
    }
}
