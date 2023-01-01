﻿using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class Program
    {
        private readonly Scanner _scanner = new Scanner();
        private readonly Compiler _compiler = new Compiler();

        public List<CompiledScript> CompiledScripts { get; } = new List<CompiledScript>();

        public ByteCodeOptimiser Optimiser { get; } = new ByteCodeOptimiser();

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

            _scanner.Reset();
            _compiler.Reset();
            Optimiser.Reset();
            
            var compiled = _compiler.Compile(_scanner, script);
            
            CompiledScripts.Add(compiled);
            Optimiser.Optimise(compiled);
            
            return compiled;
        }
    }
}
