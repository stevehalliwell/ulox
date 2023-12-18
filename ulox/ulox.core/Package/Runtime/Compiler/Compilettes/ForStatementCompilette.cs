using static ULox.CompilerState;

namespace ULox
{
    public class ForStatementCompilette : ConfigurableLoopingStatementCompilette
    {
        public ForStatementCompilette()
            : base(TokenType.FOR)
        {
        }

        protected override void BeginLoop(Compiler compiler, CompilerState.LoopState loopState)
        {
            //condition
            {
                compiler.Expression();
                compiler.ConsumeEndStatement("loop condition");

                // Jump out of the loop if the condition is false.
                compiler.EmitGotoIf(loopState.ExitLabelID);
                loopState.HasExit = true;
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
        }

        protected override void PreLoop(Compiler compiler, LoopState loopState)
        {
            compiler.TokenIterator.Consume(TokenType.OPEN_PAREN, "Expect '(' after loop with conditions.");
            //we really only want a var decl, var assign, or empty but Declaration covers everything
            compiler.Declaration();
        }
    }
}
