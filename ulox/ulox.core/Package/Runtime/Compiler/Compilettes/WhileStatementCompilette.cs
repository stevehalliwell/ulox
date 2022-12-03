namespace ULox
{
    public class WhileStatementCompilette : ConfigurableLoopingStatementCompilette
    {
        public WhileStatementCompilette()
            : base(TokenType.WHILE)
        {
        }

        protected override void BeginLoop(Compiler compiler, CompilerState.LoopState loopState)
        {
            compiler.TokenIterator.Consume(TokenType.OPEN_PAREN, "Expect '(' after loop with conditions.");

            compiler.Expression();

            int exitJump = compiler.EmitJumpIf();
            loopState.loopExitPatchLocations.Add(exitJump);

            compiler.EmitOpCode(OpCode.POP);
            
            compiler.TokenIterator.Consume(TokenType.CLOSE_PAREN, "Expect ')' after loop clauses.");
        }

        protected override void PreLoop(Compiler compiler, CompilerState.LoopState loopState)
        {
        }
    }
}
