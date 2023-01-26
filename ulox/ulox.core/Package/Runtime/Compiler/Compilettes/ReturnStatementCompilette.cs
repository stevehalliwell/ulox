using System.Runtime.CompilerServices;

namespace ULox
{
    public class ReturnStatementCompilette : ICompilette
    {
        public TokenType MatchingToken => TokenType.RETURN;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReturnStatement(Compiler compiler)
        {
            if (compiler.CurrentCompilerState.functionType == FunctionType.Init)
                compiler.ThrowCompilerException("Cannot return an expression from an 'init'");

            SimpleReturnBody(compiler);

            compiler.ConsumeEndStatement();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SimpleReturnBody(Compiler compiler)
        {
            //TODO if we give all fun an implicit (retval), then we don't need this
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
        public void Process(Compiler compiler)
            => ReturnStatement(compiler);
    }
}
