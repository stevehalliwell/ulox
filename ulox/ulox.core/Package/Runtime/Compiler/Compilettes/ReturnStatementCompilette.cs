namespace ULox
{
    public class ReturnStatementCompilette :ICompilette
    {
        public TokenType Match => TokenType.RETURN;

        public void ReturnStatement(CompilerBase compiler)
        {
            //TODO refactor out
            if (compiler.CurrentCompilerState.functionType == FunctionType.Init)
                throw new CompilerException("Cannot return an expression from an 'init'.");

            if (compiler.TokenIterator.Match(TokenType.OPEN_PAREN))
                MultiReturnBody(compiler);
            else
                SimpleReturnBody(compiler);

            compiler.ConsumeEndStatement();
        }

        private void SimpleReturnBody(CompilerBase compiler)
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

        private void MultiReturnBody(CompilerBase compiler)
        {
            compiler.EmitOpAndBytes(OpCode.RETURN, (byte)ReturnMode.Begin);
            var returnCount = compiler.ExpressionList(TokenType.CLOSE_PAREN, "Expect ')' after arguments.");
            if (returnCount == 0)
                compiler.EmitOpCode(OpCode.NULL);
            compiler.EmitOpAndBytes(OpCode.RETURN, (byte)ReturnMode.End);
        }

        public void Process(CompilerBase compiler)
            => ReturnStatement(compiler);
    }
}
