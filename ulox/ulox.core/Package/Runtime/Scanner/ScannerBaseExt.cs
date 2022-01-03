namespace ULox
{
    public static partial class ScannerBaseExt
    {
        public static void AddSingleCharTokenGenerator(this ScannerBase scannerBase, char ch, TokenType tt)
        {
            scannerBase.AddCharMatchGenerator(new ConfiguredSingleCharScannerCharMatchTokenGenerator(ch, tt));
        }

        public static void AddSingleCharTokenGenerators(this ScannerBase scannerBase, params (char ch, TokenType token)[] tokens)
        {
            foreach (var item in tokens)
            {
                scannerBase.AddSingleCharTokenGenerator(item.ch, item.token);
            }
        }

        public static void AddCompoundCharTokenGenerator(this ScannerBase scannerBase, char ch, TokenType tokenFlat, TokenType tokenCompound)
        {
            scannerBase.AddCharMatchGenerator(new CompoundCharScannerCharMatchTokenGenerator(ch, tokenFlat, tokenCompound));
        }

        public static void AddCompoundCharTokenGenerators(this ScannerBase scannerBase, params (char ch, TokenType tokenFlat, TokenType tokenCompound)[] tokens)
        {
            foreach (var item in tokens)
            {
                scannerBase.AddCompoundCharTokenGenerator(item.ch, item.tokenFlat, item.tokenCompound);
            }
        }

        public static void AddDefaultGenerator(this ScannerBase scannerBase, params IScannerTokenGenerator[] scannerTokenGenerators)
        {
            foreach (var item in scannerTokenGenerators)
            {
                scannerBase.AddDefaultGenerator(item);
            }
        }

        public static void AddIdentifierGenerator(this ScannerBase scannerBase, params (string name, TokenType tokenType)[] litteralToTokens)
        {
            foreach (var item in litteralToTokens)
            {
                scannerBase.IdentifierScannerTokenGenerator.Add(item.name, item.tokenType);
            }
        }

        public static void SetupSimpleScanner(this ScannerBase scannerBase)
        {
            scannerBase.AddDefaultGenerator(
                new WhiteSpaceScannerTokenGenerator(),
                new StringScannerTokenGenerator(),
                new NumberScannerTokenGenerator()
            );

            scannerBase.AddCharMatchGenerator(new SlashScannerTokenGenerator());

            scannerBase.AddIdentifierGenerator(
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
                ("fname", TokenType.CONTEXT_NAME_FUNC)
                );

            scannerBase.AddSingleCharTokenGenerators(
                ('(', TokenType.OPEN_PAREN),
                (')', TokenType.CLOSE_PAREN),
                ('{', TokenType.OPEN_BRACE),
                ('}', TokenType.CLOSE_BRACE),
                (',', TokenType.COMMA),
                (';', TokenType.END_STATEMENT));

            scannerBase.AddCompoundCharTokenGenerators(
                ('+', TokenType.PLUS, TokenType.PLUS_EQUAL),
                ('-', TokenType.MINUS, TokenType.MINUS_EQUAL),
                ('*', TokenType.STAR, TokenType.STAR_EQUAL),
                ('%', TokenType.PERCENT, TokenType.PERCENT_EQUAL),
                ('!', TokenType.BANG, TokenType.BANG_EQUAL),
                ('=', TokenType.ASSIGN, TokenType.EQUALITY),
                ('<', TokenType.LESS, TokenType.LESS_EQUAL),
                ('>', TokenType.GREATER, TokenType.GREATER_EQUAL));
        }
    }
}
