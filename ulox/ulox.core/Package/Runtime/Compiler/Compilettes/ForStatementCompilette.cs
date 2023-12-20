using static ULox.CompilerState;

namespace ULox
{
    public sealed class ForStatementCompilette : ICompilette
    {
        public TokenType MatchingToken { get; } = TokenType.FOR;

        public void Process(Compiler compiler)
        {
            ConfigurableLoopingStatement(compiler);
        }

        private static void ConfigurableLoopingStatement(Compiler compiler)
        {
            compiler.BeginScope();

            var comp = compiler.CurrentCompilerState;
            var loopState = new LoopState(compiler.UniqueChunkLabelStringConstant("loop_exit"));
            comp.LoopStates.Push(loopState);

            //preloop
            compiler.TokenIterator.Consume(TokenType.OPEN_PAREN, "Expect '(' after loop with conditions.");
            //we really only want a var decl, var assign, or empty but Declaration covers everything
            compiler.Declaration();

            loopState.StartLabelID = compiler.LabelUniqueChunkLabel("loop_start");
            loopState.ContinueLabelID = loopState.StartLabelID;

            //begine loop
            var hasCondition = false;
            //condition
            {
                if (!compiler.TokenIterator.Check(TokenType.END_STATEMENT))
                {
                    hasCondition = true;
                    compiler.Expression();
                    loopState.HasExit = true;
                }
                compiler.ConsumeEndStatement("loop condition");

                // Jump out of the loop if the condition is false.
                compiler.EmitGotoIf(loopState.ExitLabelID);
                if (hasCondition)
                    compiler.EmitPop(); // Condition.
            }

            var bodyJump = compiler.GotoUniqueChunkLabel("loop_body");
            //increment
            {
                var newStartLabel = compiler.LabelUniqueChunkLabel("loop_start");
                loopState.ContinueLabelID = newStartLabel;
                if (compiler.TokenIterator.CurrentToken.TokenType != TokenType.CLOSE_PAREN)
                {
                    compiler.Expression();
                    compiler.EmitPop();
                }
                compiler.EmitGoto(loopState.StartLabelID);
                loopState.StartLabelID = newStartLabel;
                compiler.EmitLabel(bodyJump);
            }

            compiler.TokenIterator.Consume(TokenType.CLOSE_PAREN, "Expect ')' after loop clauses.");

            compiler.BeginScope();
            loopState.ScopeDepth = comp.scopeDepth;
            compiler.Statement();
            compiler.EndScope();

            compiler.EmitGoto(loopState.StartLabelID);

            if (!loopState.HasExit)
                compiler.ThrowCompilerException("Loops must contain a termination");
            compiler.EmitLabel(loopState.ExitLabelID);
            compiler.EmitPop();

            compiler.EndScope();
        }
    }
}
