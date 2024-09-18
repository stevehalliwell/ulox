using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class Program
    {
        public Scanner Scanner { get; } = new();
        public Compiler Compiler { get; } = new();
        public Optimiser Optimiser { get; } = new Optimiser();
        public TypeInfo TypeInfo => Compiler.TypeInfo;

        public List<CompiledScript> CompiledScripts { get; } = new List<CompiledScript>();

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

            var existing = CompiledScripts.Find(x => x.ScriptHash == hash);
            if (existing != null)
                return existing;

            Scanner.Reset();
            Compiler.Reset();
            Optimiser.Reset();
            
            var tokens = Scanner.Scan(script);
            var compiled = Compiler.Compile(tokens, Scanner.GetLineLengths(), script);
            
            CompiledScripts.Add(compiled);
            Optimiser.Optimise(compiled);
            
            return compiled;
        }
    }
}
