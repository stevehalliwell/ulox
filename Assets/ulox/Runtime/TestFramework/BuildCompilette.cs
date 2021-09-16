namespace ULox
{
    public enum BuildOpType : byte
    {
        Bind,
        Queue,
    }

    public class BuildCompilette : ICompilette
    {
        private const string BindIdentKeyword = "bind";
        private const string QueueIdentKeyword = "queue";

        public TokenType Match => TokenType.BUILD;

        public void Process(CompilerBase compiler)
        {
            compiler.Consume(TokenType.IDENTIFIER, "Expect identifier after build command.");

            BuildOpType buildOpType = BuildOpType.Bind;

            switch (compiler.PreviousToken.Literal)
            {
            case BindIdentKeyword:
                buildOpType = BuildOpType.Bind;
                break;

            case QueueIdentKeyword:
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
