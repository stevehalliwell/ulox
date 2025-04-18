﻿using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
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

        private Engine CreateEngine()
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
            BenchmarkRunner.Run<Program>(args: args);
        }

        //[Benchmark]
        //public void ScriptVsNativeFunctional_UloxMethods()
        //{
        //    var engine = CreateEngine();
        //    //todo need to look at the byteocode for this, see if we can speed it up
        //    engine.RunScript(new Script("", ScriptVsNativeFunctional.FunctionalUlox));
        //}

        //[Benchmark]
        //public void ScriptVsNativeFunctional_NativeMethods()
        //{
        //    var engine = CreateEngine();
        //    engine.RunScript(new Script("", ScriptVsNativeFunctional.FunctionalNative));
        //}

        //[Benchmark]
        //public void Object_PosVelUpdate()
        //{
        //    var engine = CreateEngine();
        //    engine.RunScript(new Script("", ObjectVsSoa.ObjectBasedScript));
        //}

        //[Benchmark]
        //public void Soa_PosVelUpdate()
        //{
        //    var engine = CreateEngine();
        //    engine.RunScript(new Script("", ObjectVsSoa.SoaBasedScript));
        //}

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

        //[Benchmark]
        //public CompiledScript CompileVsExecute_CompileOnly()
        //{
        //    _engine = CreateEngine();
        //    return _engine.Context.Program.Compiler.Compile(_tokenisedScript);
        //}

        [Benchmark]
        public CompiledScript CompileVsExecute_DeepCloneOnly()
        {
            _engine = CreateEngine();
            var compiled = _scriptCompiledNotOpt.DeepClone();
            return compiled;
        }

        [Benchmark]
        public CompiledScript CompileVsExecute_OptimiseOnly()
        {
            _engine = CreateEngine();
            var compiled = _scriptCompiledNotOpt.DeepClone();
            _engine.Context.Program.Optimiser.Optimise(compiled);
            return compiled;
        }

        [Benchmark]
        public void CompileVsExecute_All()
        {
            _engine = CreateEngine();
            _engine.RunScript(CompileVsExecute.Script);
        }

        //[Benchmark]
        //public void Looping_Loop()
        //{
        //    var engine = CreateEngine();
        //    engine.Context.Vm.Interpret(_loopCompiled.TopLevelChunk);
        //}

        //[Benchmark]
        //public void Conditional_If()
        //{
        //    var engine = CreateEngine();
        //    engine.Context.Vm.Interpret(_ifCompiled.TopLevelChunk);
        //}

        //[Benchmark]
        //public string Dissasm_Script()
        //{
        //    var dis = new Disassembler();
        //    dis.Iterate(_scriptCompiled);
        //    return dis.GetString();
        //}
    }
}
