namespace ULox
{
    public class LoopStatementCompilette : ConfigurableLoopingStatementCompilette
    {
        public LoopStatementCompilette() 
            : base(TokenType.LOOP)
        {
        }

        protected override int BeginLoop(Compiler compiler, int loopStart, CompilerState.LoopState loopState)
        {
            //if we have a ( then it could be a native array loop, so expand for that
            //if not it's a manual exit loop
            if (compiler.TokenIterator.Match(TokenType.OPEN_PAREN))
            {
                //temp
                compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect identifier after loop statement with arg.");
                var arrayName = compiler.TokenIterator.PreviousToken.Lexeme;
                var (arrayGetOp, _, arrayArgId) = compiler.ResolveNameLookupOpCode(arrayName);
                var itemName = "item";
                var indexName = "i";
                
                if(compiler.TokenIterator.Match(TokenType.COMMA))
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
                    var prevToken = compiler.TokenIterator.PreviousToken;
                    compiler.ThrowCompilerException($"Loop error: itemName '{itemName}' already exists at this scope, name given to loop must be unique");
                }
                if (compiler.CurrentCompilerState.ResolveLocal(compiler, indexName) != -1)
                {
                    var prevToken = compiler.TokenIterator.PreviousToken;
                    compiler.ThrowCompilerException($"Loop error: indexName '{indexName}' already exists at this scope, name used for index in loop must be unique");
                }

                //skip the looop if the target is null
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

                loopStart = compiler.CurrentChunkInstructinCount;
                loopState.loopContinuePoint = loopStart;
                {
                    //confirm that the index isn't over the length of the array
                    IsIndexLessThanArrayCount(compiler, arrayGetOp, arrayArgId, indexArgID);

                    //run the condition 
                    var exitJump = compiler.EmitJumpIf();
                    loopState.loopExitPatchLocations.Add(exitJump);
                    compiler.EmitOpCode(OpCode.POP); // Condition.
                }
                //increment
                int bodyJump = compiler.EmitJump();
                {
                    int incrementStart = compiler.CurrentChunkInstructinCount;
                    loopState.loopContinuePoint = incrementStart;
                    IncrementLocalByOne(compiler, indexArgID);
                    compiler.EmitLoop(loopStart);
                    loopStart = incrementStart;
                    compiler.PatchJump(bodyJump);
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

                return loopStart;
            }

            return loopStart;
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
    }
}
