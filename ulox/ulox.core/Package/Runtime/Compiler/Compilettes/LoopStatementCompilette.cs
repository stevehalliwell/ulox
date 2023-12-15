namespace ULox
{
    public class LoopStatementCompilette : ConfigurableLoopingStatementCompilette
    {
        public LoopStatementCompilette()
            : base(TokenType.LOOP)
        {
        }

        protected override void BeginLoop(Compiler compiler, CompilerState.LoopState loopState)
        {
            //if we have a { then its a inf loop
            if (compiler.TokenIterator.Check(TokenType.OPEN_BRACE))
                return;

            //temp
            compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect identifier after loop statement with arg.");
            var arrayName = compiler.TokenIterator.PreviousToken.Lexeme;
            var arrayResolveRes = compiler.ResolveNameLookupOpCode(arrayName);
            var itemName = "item";
            var itemArgID = (byte)0;
            var indexName = "i";
            var indexArgID = (byte)0;
            var countName = "count";
            var countNameID = (byte)0;

            //handle user override of loop variables with specific names
            {
                //refer to output of For_WhenLimitedIterations_ShouldPrint3Times 
                if (compiler.TokenIterator.Match(TokenType.COMMA))
                {
                    //that's the item name
                    compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect identifier after first comma in loop statement with arg.");
                    itemName = compiler.TokenIterator.PreviousToken.Lexeme;
                }
                itemArgID = compiler.DeclareAndDefineLocal(itemName, "Loop item name");

                if (compiler.TokenIterator.Match(TokenType.COMMA))
                {
                    //that's the index name
                    compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect identifier after second comma in loop statement with arg.");
                    indexName = compiler.TokenIterator.PreviousToken.Lexeme;
                }
                indexArgID = compiler.DeclareAndDefineLocal(indexName, "Loop index name");

                if (compiler.TokenIterator.Match(TokenType.COMMA))
                {
                    //that's the count name
                    compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect identifier after third comma in loop statement with arg.");
                    countName = compiler.TokenIterator.PreviousToken.Lexeme;
                }
                countNameID = compiler.DeclareAndDefineLocal(countName, "Loop count name");
            }

            //skip the loop if the target is null
            compiler.EmitPacketFromResolveGet(arrayResolveRes);
            compiler.EmitGotoIf(loopState.ExitLabelID);
            loopState.HasExit = true;
            compiler.EmitPop();

            //prep count
            compiler.EmitPacketFromResolveGet(arrayResolveRes);
            compiler.EmitPacket(new ByteCodePacket(OpCode.COUNT_OF));
            compiler.EmitPacket(new ByteCodePacket(OpCode.SET_LOCAL, countNameID));
            compiler.EmitPop();

            loopState.StartLabelID = compiler.LabelUniqueChunkLabel("loop_start");
            loopState.ContinueLabelID = loopState.StartLabelID;
            {
                //confirm that the index isn't over the length of the array
                IsIndexLessThanCount(compiler, countNameID, indexArgID);

                //run the condition 
                compiler.EmitGotoIf(loopState.ExitLabelID);
                loopState.HasExit = true;
                compiler.EmitPop(); // Condition.
            }
            //increment
            var bodyJump = compiler.GotoUniqueChunkLabel("inc_body");
            {
                var newStartLabel = compiler.LabelUniqueChunkLabel("loop_start");
                loopState.ContinueLabelID = newStartLabel;
                IncrementLocalByOne(compiler, indexArgID);
                compiler.EmitGoto(loopState.StartLabelID);
                loopState.StartLabelID = newStartLabel;
                compiler.EmitLabel(bodyJump);
            }
            //prep item infront of body
            {
                compiler.EmitPacketFromResolveGet(arrayResolveRes);
                compiler.EmitPacket(new ByteCodePacket(OpCode.GET_LOCAL, indexArgID));
                compiler.EmitPacket(new ByteCodePacket(OpCode.GET_INDEX));
                compiler.EmitPacket(new ByteCodePacket(OpCode.SET_LOCAL, itemArgID));
                compiler.EmitPop();
            }
        }

        public static void IsIndexLessThanCount(Compiler compiler, byte countArgId, byte indexArgId)
        {
            compiler.EmitPacket(new ByteCodePacket(OpCode.GET_LOCAL, indexArgId));
            compiler.EmitPacket(new ByteCodePacket(OpCode.GET_LOCAL, countArgId));
            compiler.EmitPacket(new ByteCodePacket(OpCode.LESS));
        }

        public static void IncrementLocalByOne(Compiler compiler, byte indexArgID)
        {
            compiler.EmitPacket(new ByteCodePacket(OpCode.GET_LOCAL, indexArgID));
            compiler.EmitPacket(new ByteCodePacket(new ByteCodePacket.PushValueDetails(1)));
            compiler.EmitPacket(new ByteCodePacket(OpCode.ADD));
            compiler.EmitPacket(new ByteCodePacket(OpCode.SET_LOCAL, indexArgID));
            compiler.EmitPop();
        }

        protected override void PreLoop(Compiler compiler, CompilerState.LoopState loopState)
        {
        }
    }
}
