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

        public void Process(Compiler compiler)
        {
            ConfigurableLoopingStatement(compiler);
        }

        protected void ConfigurableLoopingStatement(Compiler compiler)
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

        private int BeginLoop(Compiler compiler, int loopStart, LoopState loopState)
        {
            if (_expectsLoopParethesis)
            {
                compiler.TokenIterator.Consume(TokenType.OPEN_PAREN, "Expect '(' after loop with conditions.");

                if (_expectsPreAndPostStatements)
                {
                    ForLoopInitialisationStatement(compiler);

                    loopStart = compiler.CurrentChunkInstructinCount;
                    loopState.loopContinuePoint = loopStart;

                    if (!compiler.TokenIterator.Match(TokenType.END_STATEMENT))
                    {
                        ForLoopCondtionStatement(compiler, loopState);
                    }

                    if (!compiler.TokenIterator.Check(TokenType.CLOSE_PAREN))
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

                compiler.TokenIterator.Consume(TokenType.CLOSE_PAREN, "Expect ')' after loop clauses.");
            }

            return loopStart;
        }

        protected void ForLoopCondtionStatement(Compiler compiler, LoopState loopState)
        {
            compiler.Expression(); 
            compiler.ConsumeEndStatement("loop condition");

            // Jump out of the loop if the condition is false.
            var exitJump = compiler.EmitJump(OpCode.JUMP_IF_FALSE);
            loopState.loopExitPatchLocations.Add(exitJump);
            compiler.EmitOpCode(OpCode.POP); // Condition.
        }

        protected void ForLoopInitialisationStatement(Compiler compiler)
        {
            //we really only want a var decl, var assign, or empty but Declaration covers everything
            compiler.Declaration();
        }

        protected void PatchLoopExits(Compiler compiler, LoopState loopState)
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
