namespace ULox
{
    public class Scanner : ScannerBase
    {
        public Scanner()
        {
            this.SetupSimpleScanner();

            this.AddIdentifierGenerators(
                (".", TokenType.DOT),

                ("test", TokenType.TEST),
                ("testcase", TokenType.TESTCASE),

                ("class", TokenType.CLASS),
                ("this", TokenType.THIS),
                ("super", TokenType.SUPER),
                ("static", TokenType.STATIC),
                ("init", TokenType.INIT));

            this.AddSingleCharTokenGenerators(
                ('.', TokenType.DOT),
                (':', TokenType.COLON),
                ('?', TokenType.QUESTION));
        }
    }
}