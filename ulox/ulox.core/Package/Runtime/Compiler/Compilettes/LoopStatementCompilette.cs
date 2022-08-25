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

                if (compiler.CurrentCompilerState.ResolveLocal(itemName) != -1)
                {
                    var prevToken = compiler.TokenIterator.PreviousToken;
                    throw new CompilerException($"Loop error: itemName '{itemName}' already exists at this scope, name given to loop must be unique at {prevToken.Line}:{prevToken.Character} '{prevToken.Literal}'.");
                }
                if (compiler.CurrentCompilerState.ResolveLocal(indexName) != -1)
                {
                    var prevToken = compiler.TokenIterator.PreviousToken;
                    throw new CompilerException($"Loop error: indexName '{indexName}' already exists at this scope, name used for index in loop must be unique at {prevToken.Line}:{prevToken.Character} '{prevToken.Literal}'.");
                }

                //skip the looop if the target is null
                compiler.EmitOpAndBytes(arrayGetOp, arrayArgId);
                int exitJumpArrayIsNullLocation = compiler.EmitJumpIf();
                loopState.loopExitPatchLocations.Add(exitJumpArrayIsNullLocation);
                compiler.EmitOpCode(OpCode.POP);

                //refer to output of For_WhenLimitedIterations_ShouldPrint3Times 
                compiler.CurrentCompilerState.DeclareVariableByName(indexName);
                compiler.CurrentCompilerState.MarkInitialised();
                var indexArgID = (byte)compiler.CurrentCompilerState.ResolveLocal(indexName);
                compiler.CurrentCompilerState.DeclareVariableByName(itemName);
                compiler.CurrentCompilerState.MarkInitialised();
                var itemArgID = (byte)compiler.CurrentCompilerState.ResolveLocal(itemName);
                
                //prep i
                compiler.EmitOpAndBytes(OpCode.PUSH_BYTE, 0);
                compiler.EmitOpAndBytes(OpCode.SET_LOCAL, indexArgID);
                compiler.EmitOpCode(OpCode.NULL);
                compiler.EmitOpAndBytes(OpCode.SET_LOCAL, itemArgID);

                loopStart = compiler.CurrentChunkInstructinCount;
                loopState.loopContinuePoint = loopStart;
                {
                    //confirm that the index isn't over the length of the array
                    compiler.EmitOpAndBytes(OpCode.GET_LOCAL, indexArgID);
                    compiler.EmitOpAndBytes(arrayGetOp, arrayArgId);
                    byte lengthNameID = compiler.AddCustomStringConstant("Count");
                    compiler.EmitOpAndBytes(OpCode.INVOKE, lengthNameID, 0);
                    compiler.EmitOpAndBytes(OpCode.LESS);

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
                    compiler.EmitOpAndBytes(OpCode.GET_LOCAL, indexArgID);
                    compiler.EmitOpAndBytes(OpCode.PUSH_BYTE, 1);
                    compiler.EmitOpCode(OpCode.ADD);
                    compiler.EmitOpAndBytes(OpCode.SET_LOCAL, indexArgID);
                    compiler.EmitOpCode(OpCode.POP);
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
    }
}
