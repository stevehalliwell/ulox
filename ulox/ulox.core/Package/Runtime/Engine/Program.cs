using System.Collections.Generic;
using System.Linq;

namespace ULox
{
    public class Program : IProgram
    {
        private readonly IScanner _scanner = new Scanner();
        private readonly ICompiler _compiler = new Compiler();

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
