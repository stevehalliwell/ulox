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
            //if we have a ( then it could be a native array loop, so expand for that
            //if not it's a manual exit loop
            if (!compiler.TokenIterator.Match(TokenType.OPEN_PAREN))
                return;
            
            //temp
            compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect identifier after loop statement with arg.");
            var arrayName = compiler.TokenIterator.PreviousToken.Lexeme;
            var (arrayGetOp, _, arrayArgId) = compiler.ResolveNameLookupOpCode(arrayName);
            var itemName = "item";
            var indexName = "i";

            if (compiler.TokenIterator.Match(TokenType.COMMA))
            {
                //that's the item name
                compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect identifier after first comma in loop statement with arg.");
                itemName = compiler.TokenIterator.PreviousToken.Lexeme;
            }

            if (compiler.TokenIterator.Match(TokenType.COMMA))
            {
                //that's the index name
                compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect identifier after first comma in loop statement with arg.");
                indexName = compiler.TokenIterator.PreviousToken.Lexeme;
            }

            if (compiler.CurrentCompilerState.ResolveLocal(compiler, itemName) != -1)
            {
                compiler.ThrowCompilerException($"Loop error: itemName '{itemName}' already exists at this scope, name given to loop must be unique");
            }
            if (compiler.CurrentCompilerState.ResolveLocal(compiler, indexName) != -1)
            {
                compiler.ThrowCompilerException($"Loop error: indexName '{indexName}' already exists at this scope, name used for index in loop must be unique");
            }

            //skip the loop if the target is null
            compiler.EmitOpAndBytes(arrayGetOp, arrayArgId);
            int exitJumpArrayIsNullLocation = compiler.EmitJumpIf();
            loopState.loopExitPatchLocations.Add(exitJumpArrayIsNullLocation);
            compiler.EmitOpCode(OpCode.POP);

            //refer to output of For_WhenLimitedIterations_ShouldPrint3Times 
            compiler.CurrentCompilerState.DeclareVariableByName(compiler, indexName);
            compiler.CurrentCompilerState.MarkInitialised();
            var indexArgID = (byte)compiler.CurrentCompilerState.ResolveLocal(compiler, indexName);
            compiler.CurrentCompilerState.DeclareVariableByName(compiler, itemName);
            compiler.CurrentCompilerState.MarkInitialised();
            var itemArgID = (byte)compiler.CurrentCompilerState.ResolveLocal(compiler, itemName);

            //prep i
            compiler.EmitOpAndBytes(OpCode.PUSH_BYTE, 0);
            compiler.EmitOpAndBytes(OpCode.SET_LOCAL, indexArgID);
            compiler.EmitNULL();
            compiler.EmitOpAndBytes(OpCode.SET_LOCAL, itemArgID);

            loopState.StartLabelID = compiler.LabelUniqueChunkLabel("loop_start");
            loopState.loopContinuePoint = compiler.CurrentChunkInstructinCount;
            {
                //confirm that the index isn't over the length of the array
                IsIndexLessThanArrayCount(compiler, arrayGetOp, arrayArgId, indexArgID);

                //run the condition 
                var exitJump = compiler.EmitJumpIf();
                loopState.loopExitPatchLocations.Add(exitJump);
                compiler.EmitOpCode(OpCode.POP); // Condition.
            }
            //increment
            var bodyJump = compiler.GoToUniqueChunkLabel("inc_body");
            {
                var newStartLabel = compiler.LabelUniqueChunkLabel("loop_start");
                var incrementStart = compiler.CurrentChunkInstructinCount;
                loopState.loopContinuePoint = incrementStart;
                IncrementLocalByOne(compiler, indexArgID);
                compiler.EmitGoto(loopState.StartLabelID);
                loopState.StartLabelID = newStartLabel;
                compiler.EmitLabel(bodyJump);
            }
            compiler.TokenIterator.Consume(TokenType.CLOSE_PAREN, "Expect ')' after loop clauses.");

            //prep item infront of body
            {
                compiler.EmitOpAndBytes(arrayGetOp, arrayArgId);
                compiler.EmitOpAndBytes(OpCode.GET_LOCAL, indexArgID);
                compiler.EmitOpCode(OpCode.GET_INDEX);
                compiler.EmitOpAndBytes(OpCode.SET_LOCAL, itemArgID);
                compiler.EmitOpCode(OpCode.POP);
            }
        }

        public static void IsIndexLessThanArrayCount(Compiler compiler, OpCode arrayGetOp, byte arrayArgId, byte indexArgID)
        {
            compiler.EmitOpAndBytes(OpCode.GET_LOCAL, indexArgID);
            compiler.EmitOpAndBytes(arrayGetOp, arrayArgId);
            compiler.EmitOpCode(OpCode.COUNT_OF);
            compiler.EmitOpAndBytes(OpCode.LESS);
        }

        public static void IncrementLocalByOne(Compiler compiler, byte indexArgID)
        {
            compiler.EmitOpAndBytes(OpCode.GET_LOCAL, indexArgID);
            compiler.EmitOpAndBytes(OpCode.PUSH_BYTE, 1);
            compiler.EmitOpCode(OpCode.ADD);
            compiler.EmitOpAndBytes(OpCode.SET_LOCAL, indexArgID);
            compiler.EmitOpCode(OpCode.POP);
        }

        protected override void PreLoop(Compiler compiler, CompilerState.LoopState loopState)
        {
        }
    }
}
