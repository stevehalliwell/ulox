using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace ULox.Core.Bench
{
    [MemoryDiagnoser]
    public class Program
    {
        private CompiledScript _loopCompiled;
        private CompiledScript _ifCompiled;
        private CompiledScript _scriptCompiledAndOpt;
        private CompiledScript _scriptCompiledNotOpt;
        private TokenisedScript _tokenisedScript;
        private Engine _engine;

        public const string TryItOutFolderName = "uloxs/Samples";
        public const string NoFailFolderName = "uloxs/Tests";
        private const string ULoxScriptExtension = "*.ulox";

        internal static Engine CreateEngine()
        {
            var platform = new GenericPlatform<DirectoryLimitedPlatform, ConsolePrintPlatform>(new(new(System.Environment.CurrentDirectory)), new());
            return new Engine(new Context(new ULox.Program(), new Vm(), platform));
        }

        [GlobalSetup]
        public void Setup()
        {
            var engine = CreateEngine();
            _loopCompiled = engine.Context.CompileScript(BenchmarkScripts.Loop);
            _ifCompiled = engine.Context.CompileScript(BenchmarkScripts.If);
            _scriptCompiledAndOpt = engine.Context.CompileScript(CompileVsExecute.Script);

            engine = CreateEngine();
            var scanner = engine.Context.Program.Scanner;
            _tokenisedScript = scanner.Scan(CompileVsExecute.Script);
            _scriptCompiledNotOpt = engine.Context.Program.Compiler.Compile(_tokenisedScript);
        }

        static void Main(string[] args)
        {
            var config = DefaultConfig.Instance.AddJob(
                Job.Default
                    .WithId("TwoMinute")
                    .WithLaunchCount(1)
                    .WithWarmupCount(6)
                    .WithIterationCount(20)
                    .WithInvocationCount(64)
                    .WithUnrollFactor(1));

            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
        }

        [Benchmark]
        public void ScriptVsNativeFunctional_UloxMethods()
        {
            var engine = CreateEngine();
            //todo need to look at the byteocode for this, see if we can speed it up
            engine.RunScript(ScriptVsNativeFunctional.FunctionalUlox);
        }

        [Benchmark]
        public void ScriptVsNativeFunctional_NativeMethods()
        {
            var engine = CreateEngine();
            engine.RunScript(ScriptVsNativeFunctional.FunctionalNative);
        }

        [Benchmark]
        public void BytecodeOptimiser_CompileOnly_OptimisationsOff()
        {
            DoByteCodeEngineCompile(engine => engine.Context.Program.Optimiser.Enabled = false);
        }

        [Benchmark]
        public void BytecodeOptimiser_CompileOnly_OptimisationsOn()
        {
            DoByteCodeEngineCompile(engine => engine.Context.Program.Optimiser.Enabled = true);
        }

        private void DoByteCodeEngineCompile(Action<Engine> engineAction)
        {
            CompileScriptOnly(engineAction, Vec2Variants.Tuple);
            CompileScriptOnly(engineAction, Vec2Variants.Type);
            CompileScriptsInFolder(engineAction, TryItOutFolderName);
            CompileScriptsInFolder(engineAction, NoFailFolderName);
        }

        private void CompileScriptsInFolder(Action<Engine> engineAction, string folderName)
        {
            foreach (var file in GetScriptsInFolder(folderName))
                CompileScriptOnly(engineAction, new Script(file, File.ReadAllText(file)));
        }

        private void CompileScriptOnly(Action<Engine> engineAction, Script script)
        {
            var engine = CreateEngine();
            engineAction(engine);
            engine.Context.Program.Compile(script);
        }

        internal static string[] GetScriptsInFolder(string folderName)
        {
            var folderPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, folderName));
            return Directory.GetFiles(folderPath, ULoxScriptExtension)
                .OrderBy(x => x)
                .ToArray();
        }


        [Benchmark]
        public Engine CompileVsExecute_NewEngineOnly()
        {
            _engine = CreateEngine();
            return _engine;
        }

        [Benchmark]
        public TokenisedScript CompileVsExecute_TokeniseOnly()
        {
            _engine = CreateEngine();
            return _engine.Context.Program.Scanner.Scan(CompileVsExecute.Script);
        }

        [Benchmark]
        public CompiledScript CompileVsExecute_CompileOnly()
        {
            _engine = CreateEngine();
            return _engine.Context.Program.Compiler.Compile(_tokenisedScript);
        }

        [Benchmark]
        public CompiledScript CompileVsExecute_DeepCloneOnly()
        {
            _engine = CreateEngine();
            var compiled = _scriptCompiledNotOpt.DeepClone();
            return compiled;
        }

        [Benchmark]
        public void CompileVsExecute_All()
        {
            _engine = CreateEngine();
            _engine.RunScript(CompileVsExecute.Script);
        }

        [Benchmark]
        public void Looping_Loop()
        {
            var engine = CreateEngine();
            engine.Context.Vm.Interpret(_loopCompiled.TopLevelChunk);
        }

        [Benchmark]
        public void Conditional_If()
        {
            var engine = CreateEngine();
            engine.Context.Vm.Interpret(_ifCompiled.TopLevelChunk);
        }

        [Benchmark]
        public string Dissasm_Script()
        {
            var dis = new Disassembler();
            dis.Iterate(_scriptCompiledAndOpt);
            return dis.GetString();
        }
    }
}
