using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class BuildCompilette : ICompilette
    {
        public TokenType MatchingToken => TokenType.BUILD;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Process(Compiler compiler)
        {
            do
            {
                compiler.Expression();
                compiler.EmitPacket(OpCode.BUILD);
            } while (compiler.TokenIterator.Match(TokenType.COMMA));

            compiler.ConsumeEndStatement("build command identifier(s)");
        }
    }
}
