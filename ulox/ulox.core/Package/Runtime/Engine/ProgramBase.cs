using System.Collections.Generic;
using System.Linq;

namespace ULox
{
    public class ProgramBase<TCompiler, TDisass> : IProgram
        where TCompiler : ICompiler, new()
        where TDisass : IDisassembler, new()
    {
        private readonly IScanner _scanner;
        private readonly TCompiler _compiler = new TCompiler();

        public ProgramBase(IScanner scanner)
        {
            _scanner = scanner;
        }

        public List<CompiledScript> CompiledScripts { get; private set; } = new List<CompiledScript>();

        public string Disassembly
        {
            get
            {
                var dis = new TDisass();
                foreach (var compiledScript in CompiledScripts)
                {
                    dis.DoChunk(compiledScript.TopLevelChunk);
                }

                return dis.GetString();
            }
        }

        public CompiledScript Compile(string script)
        {
            var hash = script.GetHashCode();

            var existing = CompiledScripts.FirstOrDefault(x => x.ScriptHash == hash);
            if (existing != null)
                return existing;

            _scanner.Reset();
            _compiler.Reset();

            var tokens = _scanner.Scan(script);
            var chunk = _compiler.Compile(tokens);
            var compiled = new CompiledScript(chunk, hash);
            CompiledScripts.Add(compiled);
            return compiled;
        }
    }
}
