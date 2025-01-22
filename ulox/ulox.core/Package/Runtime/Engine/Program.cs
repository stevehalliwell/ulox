using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class Program
    {
        public Scanner Scanner { get; } = new();
        public Compiler Compiler { get; } = new();
        public Optimiser Optimiser { get; } = new();
        public TypeInfo TypeInfo => Compiler.TypeInfo;

        public List<CompiledScript> CompiledScripts { get; } = new ();

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
            var existing = CompiledScripts.Find(x => x.ScriptHash == script.ScriptHash);
            if (existing != null)
                return existing;

            Scanner.Reset();
            Compiler.Reset();
            Optimiser.Reset();
            
            var tokenisedScript = Scanner.Scan(script);
            var compiled = Compiler.Compile(tokenisedScript);
            
            CompiledScripts.Add(compiled);
            Optimiser.Optimise(compiled);
            
            return compiled;
        }
    }
}
