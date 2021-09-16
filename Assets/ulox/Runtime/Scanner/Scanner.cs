namespace ULox
{
    public class Scanner : ScannerBase
    {
        public Scanner()
        {
            this.SetupSimpleScanner();

            this.AddIdentifierGenerators(
                (".", TokenType.DOT),

                ("build", TokenType.BUILD),

                ("test", TokenType.TEST),
                ("testcase", TokenType.TESTCASE),
                ("tname", TokenType.CONTEXT_NAME_TEST),
                ("tsname", TokenType.CONTEXT_NAME_TESTCASE),

                ("class", TokenType.CLASS),
                ("this", TokenType.THIS),
                ("super", TokenType.SUPER),
                ("static", TokenType.STATIC),
                ("init", TokenType.INIT),
                ("cname", TokenType.CONTEXT_NAME_CLASS)
                );

            this.AddSingleCharTokenGenerators(
                ('.', TokenType.DOT),
                (':', TokenType.COLON),
                ('?', TokenType.QUESTION));
        }
    }
}
