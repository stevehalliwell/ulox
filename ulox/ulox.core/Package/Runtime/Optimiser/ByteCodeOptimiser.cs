using System;

namespace ULox
{
    public sealed class ByteCodeOptimiser : ByteCodeIterator, IByteCodeOptimiser
    {
        public bool Enabled { get; set; } = true;

        public void Optimise(CompiledScript compiledScript)
        {
            if(!Enabled)
                return;

            Iterate(compiledScript);
        }

        public void Reset()
        {
        }

        protected override void DefaultOpCode(Chunk chunk, int i, OpCode opCode)
        {
           
        }

        protected override void DefaultPostOpCode()
        {
           
        }

        protected override void PostChunkIterate(CompiledScript compiledScript, Chunk chunk)
        {
           
        }

        protected override void PreChunkInterate(CompiledScript compiledScript, Chunk chunk)
        {
           
        }

        protected override void ProcessOp(OpCode opCode)
        {
           
        }

        protected override void ProcessOpAndByte(OpCode opCode, byte b)
        {
           
        }

        protected override void ProcessOpAndStringConstant(OpCode opCode, byte b, Value value)
        {
           
        }

        protected override void ProcessOpAndStringConstantAndByte(OpCode opCode, byte stringConstant, Value value, byte b)
        {
           
        }

        protected override void ProcessOpAndStringConstantAndByteAndUShort(OpCode opCode, byte stringConstant, Value value, byte b, ushort ushortValue)
        {
           
        }

        protected override void ProcessOpAndUShort(OpCode opCode, ushort ushortValue)
        {
           
        }

        protected override void ProcessOpClosure(OpCode opCode, byte funcID, Chunk asChunk, int upValueCount)
        {
           
        }

        protected override void ProcessOpClosureUpValue(OpCode opCode, byte fundID, int count, int upVal, byte isLocal, byte upvalIndex)
        {
           
        }

        protected override void ProcessTestOp(OpCode opCode, TestOpType testOpType)
        {
           
        }

        protected override void ProcessTestOpAndByteAndByte(OpCode opCode, TestOpType testOpType, byte b1, byte b2)
        {
           
        }

        protected override void ProcessTestOpAndStringConstantAndByte(OpCode opCode, TestOpType testOpType, byte stringConstant, Value value, byte b)
        {
           
        }

        protected override void ProcessTestOpAndStringConstantAndTestCount(OpCode opCode, byte stringConstantID, Value value, byte testCount)
        {
           
        }

        protected override void ProcessTestOpAndStringConstantAndTestCountAndTestIndexAndTestLocation(OpCode opCode, byte sc, Value value, byte testCount, int it, ushort ushortValue)
        {
           
        }

        protected override void ProcessTestOpAndUShort(OpCode opCode, TestOpType testOpType, ushort ushortValue)
        {
           
        }
    }
}
