namespace ULox
{
    //todo squash all down into scanner and use the ext to make factories
    public class Scanner : ScannerBase
    {
        public Scanner()
        {
            this.SetupSimpleScanner();

            this.AddIdentifierGenerator(
                ("build", TokenType.BUILD),

                ("test", TokenType.TEST),
                ("testcase", TokenType.TESTCASE),
                ("tcname", TokenType.CONTEXT_NAME_TESTCASE),
                ("tsname", TokenType.CONTEXT_NAME_TESTSET),

                ("class", TokenType.CLASS),
                ("mixin", TokenType.MIXIN),
                ("this", TokenType.THIS),
                ("super", TokenType.SUPER),
                ("static", TokenType.STATIC),
                ("init", TokenType.INIT),
                ("cname", TokenType.CONTEXT_NAME_CLASS),
                ("freeze", TokenType.FREEZE),

                ("inject", TokenType.INJECT),
                ("register", TokenType.REGISTER)
                );

            this.AddSingleCharTokenGenerators(
                ('.', TokenType.DOT),
                (':', TokenType.COLON),
                ('?', TokenType.QUESTION));
        }
    }
}
