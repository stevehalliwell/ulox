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

            compiler.EmitReturn();

            compiler.ConsumeEndStatement();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Process(Compiler compiler)
            => ReturnStatement(compiler);
    }
}
