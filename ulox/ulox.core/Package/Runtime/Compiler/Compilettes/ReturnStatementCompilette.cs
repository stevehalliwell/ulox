namespace ULox
{
    public class ReturnStatementCompilette :ICompilette
    {
        public TokenType Match => TokenType.RETURN;

        public void ReturnStatement(Compiler compiler)
        {
            //TODO refactor out
            if (compiler.CurrentCompilerState.functionType == FunctionType.Init)
                compiler.ThrowCompilerException("Cannot return an expression from an 'init'");

            if (compiler.TokenIterator.Match(TokenType.OPEN_PAREN))
                MultiReturnBody(compiler);
            else
                SimpleReturnBody(compiler);

            compiler.ConsumeEndStatement();
        }

        private void SimpleReturnBody(Compiler compiler)
        {
            if (compiler.TokenIterator.Check(TokenType.END_STATEMENT))
            {
                compiler.EmitReturn();
            }
            else
            {
                compiler.Expression();
                compiler.EmitOpAndBytes(OpCode.RETURN, (byte)ReturnMode.One);
            }
        }

        private void MultiReturnBody(Compiler compiler)
        {
            compiler.EmitOpAndBytes(OpCode.RETURN, (byte)ReturnMode.Begin);
            var returnCount = compiler.ExpressionList(TokenType.CLOSE_PAREN, "Expect ')' after arguments.");
            if (returnCount == 0)
                compiler.EmitNULL();
            compiler.EmitOpAndBytes(OpCode.RETURN, (byte)ReturnMode.End);
        }

        public void Process(Compiler compiler)
            => ReturnStatement(compiler);
    }
}
