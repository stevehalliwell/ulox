namespace ULox
{
    public abstract class ConfigurableLoopingStatementCompilette : ICompilette
    {
        public TokenType Match { get; private set; }
        private bool _expectsPreAndPostStatements;
        private bool _expectsLoopParethesis;

        protected ConfigurableLoopingStatementCompilette(
            TokenType match,
            bool expectsLoopParethesis,
            bool expectsPreAndPostStatements)
        {
            Match = match;
            _expectsLoopParethesis = expectsLoopParethesis;
            _expectsPreAndPostStatements = expectsPreAndPostStatements;
        }

        public void Process(CompilerBase compiler)
        {
            ConfigurableLoopingStatement(compiler);
        }

        protected void ConfigurableLoopingStatement(CompilerBase compiler)
        {
            compiler.BeginScope();

            var comp = compiler.CurrentCompilerState;
            int loopStart = compiler.CurrentChunkInstructinCount;
            var loopState = new LoopState();
            comp.loopStates.Push(loopState);
            loopState.loopContinuePoint = loopStart;

            loopStart = BeginLoop(compiler, loopStart, loopState);

            compiler.Statement();

            compiler.EmitLoop(loopStart);

            PatchLoopExits(compiler, loopState);

            compiler.EmitOpCode(OpCode.POP);

            compiler.EndScope();
        }

        private int BeginLoop(CompilerBase compiler, int loopStart, LoopState loopState)
        {
            if (_expectsLoopParethesis)
            {
                compiler.Consume(TokenType.OPEN_PAREN, "Expect '(' after loop with conditions.");

                if (_expectsPreAndPostStatements)
                {
                    ForLoopInitialisationStatement(compiler);

                    loopStart = compiler.CurrentChunkInstructinCount;
                    loopState.loopContinuePoint = loopStart;

                    if (!compiler.Match(TokenType.END_STATEMENT))
                    {
                        ForLoopCondtionStatement(compiler, loopState);
                    }

                    if (!compiler.Check(TokenType.CLOSE_PAREN))
                    {
                        int bodyJump = compiler.EmitJump(OpCode.JUMP);

                        int incrementStart = compiler.CurrentChunkInstructinCount;
                        loopState.loopContinuePoint = incrementStart;
                        compiler.Expression();
                        compiler.EmitOpCode(OpCode.POP);

                        //TODO: shouldn't you be able to omit the post loop action and have it work. this seems like it breaks it.
                        compiler.EmitLoop(loopStart);
                        loopStart = incrementStart;
                        compiler.PatchJump(bodyJump);
                    }
                }
                else
                {
                    compiler.Expression();

                    int exitJump = compiler.EmitJump(OpCode.JUMP_IF_FALSE);
                    loopState.loopExitPatchLocations.Add(exitJump);

                    compiler.EmitOpCode(OpCode.POP);
                }

                compiler.Consume(TokenType.CLOSE_PAREN, "Expect ')' after loop clauses.");
            }

            return loopStart;
        }

        protected void ForLoopCondtionStatement(CompilerBase compiler, LoopState loopState)
        {
            compiler.Expression();
            compiler.Consume(TokenType.END_STATEMENT, "Expect ';' after loop condition.");

            // Jump out of the loop if the condition is false.
            var exitJump = compiler.EmitJump(OpCode.JUMP_IF_FALSE);
            loopState.loopExitPatchLocations.Add(exitJump);
            compiler.EmitOpCode(OpCode.POP); // Condition.
        }

        protected void ForLoopInitialisationStatement(CompilerBase compiler)
        {
            //TODO if we introduce void statements ";" is a valid line of code with no ops emitted
            //  then this becomes simply, compiler.Statement();
            if (compiler.Match(TokenType.END_STATEMENT))
            {
                // No initializer.
            }
            else if (compiler.Match(TokenType.VAR))
            {
                compiler.VarDeclaration(compiler);
            }
            else
            {
                compiler.ExpressionStatement();
            }
        }

        protected void PatchLoopExits(CompilerBase compiler, LoopState loopState)
        {
            if (loopState.loopExitPatchLocations.Count == 0)
                throw new CompilerException("Loops must contain an termination.");

            for (int i = 0; i < loopState.loopExitPatchLocations.Count; i++)
            {
                compiler.PatchJump(loopState.loopExitPatchLocations[i]);
            }
        }
    }
}
