using static ULox.Optimiser;

namespace ULox
{
    public sealed class OptimiserPreenGetLocalsPass : IOptimiserPass
    {
        private int _lastModifiedIndex = -1;

        public void Prepare(Optimiser optimiser, Chunk chunk)
        {
            _lastModifiedIndex = -1;
        }

        public void ProcessPacket(Optimiser optimiser, Chunk chunk, int inst, ByteCodePacket packet)
        {
            switch (packet.OpCode)
            {
            case OpCode.ADD:
            case OpCode.MULTIPLY:
            case OpCode.EQUAL:
                if (inst < 2) return; //need at least 2 instructions to do anything

                //these are commutative so we can swap the order of the operands if it might make future passes easier/possible
                var prev = chunk.Instructions[inst - 1];
                var prevPrev = chunk.Instructions[inst - 2];

                if (prevPrev.OpCode == OpCode.GET_LOCAL && _lastModifiedIndex != inst - 2)
                {
                    switch (prev.OpCode)
                    {
                    case OpCode.PUSH_VALUE:
                    case OpCode.PUSH_CONSTANT:
                        //we can swap those two instructions
                        chunk.Instructions[inst - 2] = prev;
                        chunk.Instructions[inst - 1] = prevPrev;
                        _lastModifiedIndex = inst - 1;
                        break;
                    }
                }
                break;
            }
        }

        public PassCompleteRequest Complete(Optimiser optimiser, Chunk chunk)
        {
            return PassCompleteRequest.None;
        }
    }
}