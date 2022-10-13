using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class Program : IProgram
    {
        private readonly IScanner _scanner = new Scanner();
        private readonly ICompiler _compiler = new Compiler();
        private readonly IByteCodeOptimiser _optimiser = new ByteCodeOptimiser();

        public List<CompiledScript> CompiledScripts { get; private set; } = new List<CompiledScript>();

        public string Disassembly
        {
            get
            {
                var dis = new Disassembler();
                foreach (var compiledScript in CompiledScripts)
                {
                    dis.DoChunk(compiledScript.TopLevelChunk);
                }

                return dis.GetString();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CompiledScript Compile(Script script)
        {
            var hash = script.GetHashCode();

            var existing = CompiledScripts.FirstOrDefault(x => x.ScriptHash == hash);
            if (existing != null)
                return existing;

            _scanner.Reset();
            _compiler.Reset();
            _optimiser.Reset();

            var tokens = _scanner.Scan(script);
            var compiled = _compiler.Compile(tokens, script);
            
            CompiledScripts.Add(compiled);
            _optimiser.Optimise(compiled);
            
            return compiled;
        }
    }
}
