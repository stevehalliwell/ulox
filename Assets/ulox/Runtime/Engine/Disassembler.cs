namespace ULox
{
    public class Disassembler : DisassemblerBase
    {
        public Disassembler()
        {
            opCodeHandlers[(int)OpCode.CLASS] = AppendStringConstantThenSpaceThenUshort;
            opCodeHandlers[(int)OpCode.GET_PROPERTY] = AppendStringConstant;
            opCodeHandlers[(int)OpCode.SET_PROPERTY] = AppendStringConstant;
            opCodeHandlers[(int)OpCode.GET_SUPER] = AppendStringConstant;
            opCodeHandlers[(int)OpCode.METHOD] = AppendStringConstant;
            opCodeHandlers[(int)OpCode.INHERIT] = AppendNothing;
            opCodeHandlers[(int)OpCode.MIXIN] = AppendNothing;

            opCodeHandlers[(int)OpCode.FREEZE] = AppendNothing;

            opCodeHandlers[(int)OpCode.SUPER_INVOKE] = AppendStringConstantThenByte;
            opCodeHandlers[(int)OpCode.INVOKE] = AppendStringConstantThenByte;

            opCodeHandlers[(int)OpCode.TEST] = HandleTestOpCode;

            opCodeHandlers[(int)OpCode.BUILD] = AppendByteThenSpaceThenStringConstant;

            opCodeHandlers[(int)OpCode.REGISTER] = AppendStringConstant;
            opCodeHandlers[(int)OpCode.INJECT] = AppendStringConstant;
        }

        private int HandleTestOpCode(Chunk chunk, int i)
        {
            AppendSpace();
            i++;
            var testOpType = (TestOpType)chunk.Instructions[i];
            Append(testOpType.ToString());
            AppendSpace();
            switch (testOpType)
            {
                case TestOpType.CaseStart:
                case TestOpType.CaseEnd:
                    i = AppendStringConstant(chunk, i);
                    i = AppendByte(chunk, i);
                    break;

                case TestOpType.TestSetStart:
                    i = AppendStringConstant(chunk, i);
                    Append(" ");
                    i++;
                    var testCount = chunk.Instructions[i];
                    Append(" [");
                    for (int it = 0; it < testCount; it++)
                    {
                        i = AppendUShort(chunk, i);
                        if (it < testCount - 1)
                            Append(", ");
                    }
                    Append("] ");
                    break;

                case TestOpType.TestSetEnd:
                    i = AppendByte(chunk, i);
                    i = AppendByte(chunk, i);
                    break;
            }

            return i;
        }

        private int AppendStringConstantThenSpaceThenUshort(Chunk chunk, int i)
        {
            i = AppendStringConstant(chunk, i);
            AppendSpace();
            return AppendUShort(chunk, i);
        }

        private int AppendByteThenSpaceThenStringConstant(Chunk chunk, int i)
        {
            i = AppendByte(chunk, i);
            AppendSpace();
            return AppendStringConstant(chunk, i);
        }

        private int AppendStringConstantThenByte(Chunk chunk, int i)
        {
            i = AppendStringConstant(chunk, i);
            return AppendByte(chunk, i);
        }
    }
}
