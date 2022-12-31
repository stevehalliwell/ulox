using System.Runtime.CompilerServices;

namespace ULox
{
    public class ReturnStatementCompilette : ICompilette
    {
        public TokenType Match => TokenType.RETURN;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SimpleReturnBody(Compiler compiler)
        {
            if (compiler.TokenIterator.Check(TokenType.END_STATEMENT))
            {
                compiler.EmitReturn();
            }
            else
            {
                compiler.Expression();
                compiler.EmitPacket(new ByteCodePacket(OpCode.RETURN, ReturnMode.One));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MultiReturnBody(Compiler compiler)
        {
            compiler.EmitPacket(new ByteCodePacket(OpCode.RETURN, ReturnMode.Begin));
            var returnCount = compiler.ExpressionList(TokenType.CLOSE_PAREN, "Expect ')' after arguments.");
            if (returnCount == 0)
                compiler.EmitNULL();
            compiler.EmitPacket(new ByteCodePacket(OpCode.RETURN, ReturnMode.End));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Process(Compiler compiler)
            => ReturnStatement(compiler);
    }
}
