using static ULox.CompilerState;

namespace ULox
{
    public class ForStatementCompilette : ConfigurableLoopingStatementCompilette
    {
        public ForStatementCompilette()
            : base(TokenType.FOR)
        {
        }

        protected override int BeginLoop(Compiler compiler, int loopStart, CompilerState.LoopState loopState)
        {
            compiler.TokenIterator.Consume(TokenType.OPEN_PAREN, "Expect '(' after loop with conditions.");

            ForLoopInitialisationStatement(compiler);

            loopStart = compiler.CurrentChunkInstructinCount;
            loopState.loopContinuePoint = loopStart;

            if (!compiler.TokenIterator.Match(TokenType.END_STATEMENT))
            {
                ForLoopCondtionStatement(compiler, loopState);
            }

            if (!compiler.TokenIterator.Check(TokenType.CLOSE_PAREN))
            {
                int bodyJump = compiler.EmitJump();

                int incrementStart = compiler.CurrentChunkInstructinCount;
                loopState.loopContinuePoint = incrementStart;
                compiler.Expression();
                compiler.EmitOpCode(OpCode.POP);

                //TODO: shouldn't you be able to omit the post loop action and have it work. this seems like it breaks it.
                compiler.EmitLoop(loopStart);
                loopStart = incrementStart;
                compiler.PatchJump(bodyJump);
            }

            compiler.TokenIterator.Consume(TokenType.CLOSE_PAREN, "Expect ')' after loop clauses.");

            return loopStart;
        }

        protected void ForLoopCondtionStatement(Compiler compiler, LoopState loopState)
        {
            compiler.Expression();
            compiler.ConsumeEndStatement("loop condition");

            // Jump out of the loop if the condition is false.
            var exitJump = compiler.EmitJumpIf();
            loopState.loopExitPatchLocations.Add(exitJump);
            compiler.EmitOpCode(OpCode.POP); // Condition.
        }

        protected void ForLoopInitialisationStatement(Compiler compiler)
        {
            //we really only want a var decl, var assign, or empty but Declaration covers everything
            compiler.Declaration();
        }
    }
}
