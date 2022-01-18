namespace ULox
{
    public static class ScannerFactory
    {
        public static IScanner CreateScanner()
        {
            var scanner = new Scanner();

            var identScannerGen = new IdentifierScannerTokenGenerator();

            scanner.AddGenerators(
                new WhiteSpaceScannerTokenGenerator(),
                new StringScannerTokenGenerator(),
                new NumberScannerTokenGenerator(),
                new SlashScannerTokenGenerator(),
                new CompoundCharScannerCharMatchTokenGenerator(),
                identScannerGen
                                    );

            identScannerGen.Add(
                ("var", TokenType.VAR),
                ("string", TokenType.STRING),
                ("int", TokenType.INT),
                ("float", TokenType.FLOAT),
                ("and", TokenType.AND),
                ("or", TokenType.OR),
                ("if", TokenType.IF),
                ("else", TokenType.ELSE),
                ("while", TokenType.WHILE),
                ("for", TokenType.FOR),
                ("loop", TokenType.LOOP),
                ("return", TokenType.RETURN),
                ("break", TokenType.BREAK),
                ("continue", TokenType.CONTINUE),
                ("true", TokenType.TRUE),
                ("false", TokenType.FALSE),
                ("null", TokenType.NULL),
                ("fun", TokenType.FUNCTION),
                ("throw", TokenType.THROW),
                ("yield", TokenType.YIELD),
                ("fname", TokenType.CONTEXT_NAME_FUNC),

                ("test", TokenType.TEST),
                ("testcase", TokenType.TESTCASE)
                                              );

            scanner.AddSingleCharTokenGenerators(
                ('(', TokenType.OPEN_PAREN),
                (')', TokenType.CLOSE_PAREN),
                ('{', TokenType.OPEN_BRACE),
                ('}', TokenType.CLOSE_BRACE),
                (',', TokenType.COMMA),
                (';', TokenType.END_STATEMENT),
                ('.', TokenType.DOT)
                                                    );

            identScannerGen.Add(
                ("build", TokenType.BUILD),

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

            scanner.AddSingleCharTokenGenerators(
                (':', TokenType.COLON),
                ('?', TokenType.QUESTION));

            return scanner;
        }

        public static IScanner CreateSimpleScanner()
        {
            var scanner = new Scanner();

            var identScannerGen = new IdentifierScannerTokenGenerator();

            scanner.AddGenerators(
                new WhiteSpaceScannerTokenGenerator(),
                new StringScannerTokenGenerator(),
                new NumberScannerTokenGenerator(),
                new SlashScannerTokenGenerator(),
                new CompoundCharScannerCharMatchTokenGenerator(),
                identScannerGen
                                    );

            identScannerGen.Add(
                ("var", TokenType.VAR),
                ("string", TokenType.STRING),
                ("int", TokenType.INT),
                ("float", TokenType.FLOAT),
                ("and", TokenType.AND),
                ("or", TokenType.OR),
                ("if", TokenType.IF),
                ("else", TokenType.ELSE),
                ("while", TokenType.WHILE),
                ("for", TokenType.FOR),
                ("loop", TokenType.LOOP),
                ("return", TokenType.RETURN),
                ("break", TokenType.BREAK),
                ("continue", TokenType.CONTINUE),
                ("true", TokenType.TRUE),
                ("false", TokenType.FALSE),
                ("null", TokenType.NULL),
                ("fun", TokenType.FUNCTION),
                ("throw", TokenType.THROW),
                ("yield", TokenType.YIELD),
                ("fname", TokenType.CONTEXT_NAME_FUNC),

                ("test", TokenType.TEST),
                ("testcase", TokenType.TESTCASE)
                                              );

            scanner.AddSingleCharTokenGenerators(
                ('(', TokenType.OPEN_PAREN),
                (')', TokenType.CLOSE_PAREN),
                ('{', TokenType.OPEN_BRACE),
                ('}', TokenType.CLOSE_BRACE),
                (',', TokenType.COMMA),
                (';', TokenType.END_STATEMENT),
                ('.', TokenType.DOT)
                                                    );

            return scanner;
        }
    }
}
