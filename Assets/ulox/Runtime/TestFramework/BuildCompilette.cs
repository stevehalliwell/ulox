namespace ULox
{
    public enum BuildOpType : byte
    {
        Bind,
        Queue,
    }

    public class BuildCompilette : ICompilette
    {
        public TokenType Match => TokenType.BUILD;

        public void Process(CompilerBase compiler)
        {
            BuildOpType buildOpType = BuildOpType.Bind;

            compiler.Consume(TokenType.IDENTIFIER, "Expect identifier after build command.");

            switch (compiler.PreviousToken.Literal)
            {
            case "bind":
                buildOpType = BuildOpType.Bind;
                break;
            case "queue":
                buildOpType = BuildOpType.Queue;
                break;
            default:
                throw new CompilerException($"'build' keyword followed by unexpected identifier '{compiler.PreviousToken.Literal}'.");
            }

            //read the rest of the constants and write out build opcodes
            do
            {
                compiler.Consume(TokenType.STRING, "Expect string after build op type.");
                var ident = (string)compiler.PreviousToken.Literal;
                var identId = compiler.CurrentChunk.AddConstant(Value.New(ident));
                compiler.EmitOpAndBytes(OpCode.BUILD, (byte)buildOpType, identId);
            } while (compiler.Match(TokenType.COMMA));

            compiler.Consume(TokenType.END_STATEMENT, "Expect end of statement after build command identifier(s).");
        }
    }
}
