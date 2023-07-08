namespace ULox
{
    public sealed class BuildCompilette : ICompilette
    {
        public TokenType MatchingToken => TokenType.BUILD;

        public void Process(Compiler compiler)
        {
            do
            {
                compiler.Expression();
                compiler.EmitPacket(new ByteCodePacket(OpCode.BUILD));
            } while (compiler.TokenIterator.Match(TokenType.COMMA));

            compiler.ConsumeEndStatement("build command identifier(s)");
        }
    }
}
