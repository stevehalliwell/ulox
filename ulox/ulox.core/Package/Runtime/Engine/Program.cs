using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class Program
    {
        private readonly Scanner _scanner = new Scanner();
        private readonly Compiler _compiler = new Compiler();
        private readonly ByteCodeOptimiser _optimiser = new ByteCodeOptimiser();

        public List<CompiledScript> CompiledScripts { get; private set; } = new List<CompiledScript>();

        public ByteCodeOptimiser Optimiser => _optimiser;

        public string Disassembly
        {
            get
            {
                var dis = new Disassembler();
                foreach (var compiledScript in CompiledScripts)
                {
                    dis.Iterate(compiledScript);
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
