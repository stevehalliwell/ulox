namespace ULox
{
    public class BuildCompilette : ICompilette
    {
        private const string BindIdentKeyword = "bind";
        private const string QueueIdentKeyword = "queue";

        public TokenType Match => TokenType.BUILD;

        public void Process(Compiler compiler)
        {
            compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect identifier after build command.");

            var buildOpType = BuildOpType.Bind;

            string lexeme = compiler.TokenIterator.PreviousToken.Lexeme;
            switch (lexeme)
            {
            case BindIdentKeyword:
                buildOpType = BuildOpType.Bind;
                break;

            case QueueIdentKeyword:
                buildOpType = BuildOpType.Queue;
                break;

            default:
                throw new CompilerException($"'build' keyword followed by unexpected identifier '{lexeme}'.");
            }

            //read the rest of the constants and write out build opcodes
            do
            {
                compiler.Expression();
                compiler.EmitOpAndBytes(OpCode.BUILD, (byte)buildOpType);
            } while (compiler.TokenIterator.Match(TokenType.COMMA));

            compiler.ConsumeEndStatement("build command identifier(s)");
        }
    }
}
