using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class BuildCompilette : ICompilette
    {
        public TokenType Match => TokenType.BUILD;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Process(Compiler compiler)
        {
            do
            {
                compiler.Expression();
                compiler.EmitOpCode(OpCode.BUILD);
            } while (compiler.TokenIterator.Match(TokenType.COMMA));

            compiler.ConsumeEndStatement("build command identifier(s)");
        }
    }
}
