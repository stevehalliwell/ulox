namespace ULox
{
    public static partial class ScannerBaseExt
    {
        public static void AddSingleCharTokenGenerator(this IScanner scanner, char ch, TokenType tt)
        {
            scanner.AddGenerator(new ConfiguredSingleCharScannerCharMatchTokenGenerator(ch, tt));
        }

        public static void AddSingleCharTokenGenerators(this IScanner scanner, params (char ch, TokenType token)[] tokens)
        {
            foreach (var item in tokens)
            {
                scanner.AddSingleCharTokenGenerator(item.ch, item.token);
            }
        }

        public static void AddGenerators(this IScanner scanner, params IScannerTokenGenerator[] scannerTokenGenerators)
        {
            foreach (var item in scannerTokenGenerators)
            {
                scanner.AddGenerator(item);
            }
        }

        public static void AddIdentifierGenerator(this IScanner scanner, params (string name, TokenType tokenType)[] litteralToTokens)
        {
            foreach (var item in litteralToTokens)
            {
                scanner.IdentifierScannerTokenGenerator.Add(item.name, item.tokenType);
            }
        }

        public static void SetupSimpleScanner(this IScanner scanner)
        {
            scanner.AddGenerators(
                new WhiteSpaceScannerTokenGenerator(),
                new StringScannerTokenGenerator(),
                new NumberScannerTokenGenerator(),
                new SlashScannerTokenGenerator(),
                new CompoundCharScannerCharMatchTokenGenerator()
                                    );

            scanner.AddIdentifierGenerator(
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
                (".", TokenType.DOT)
                                              );

            scanner.AddSingleCharTokenGenerators(
                ('(', TokenType.OPEN_PAREN),
                (')', TokenType.CLOSE_PAREN),
                ('{', TokenType.OPEN_BRACE),
                ('}', TokenType.CLOSE_BRACE),
                (',', TokenType.COMMA),
                (';', TokenType.END_STATEMENT)
                                                    );
        }
    }
}
