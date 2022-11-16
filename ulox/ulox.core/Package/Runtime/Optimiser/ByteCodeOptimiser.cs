using System;

namespace ULox
{
    public sealed class ByteCodeOptimiser : IByteCodeOptimiser
    {
        public bool Enabled { get; set; } = true;

        public void Optimise(CompiledScript compiledScript)
        {
            if(!Enabled)
                return;

            
        }

        public void Reset()
        {
        }
    }
}
