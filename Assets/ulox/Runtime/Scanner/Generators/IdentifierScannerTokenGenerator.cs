using System.Collections.Generic;
using System.Text;

namespace ULox
{
    public class IdentifierScannerTokenGenerator : IScannerTokenGenerator
    {
        private StringBuilder workingSpaceStringBuilder = new StringBuilder();

        private readonly Dictionary<string, TokenType> keywords = new Dictionary<string, TokenType>()
        {
            { "var",    TokenType.VAR},
            { "string", TokenType.STRING},
            { "int",    TokenType.INT},
            { "float",  TokenType.FLOAT},
            { "and",    TokenType.AND},
            { "or",     TokenType.OR},
            { "if",     TokenType.IF},
            { "else",   TokenType.ELSE},
            { "while",  TokenType.WHILE},
            { "for",    TokenType.FOR},
            { "loop",   TokenType.LOOP},
            { "return", TokenType.RETURN},
            { "break",  TokenType.BREAK},
            { "continue", TokenType.CONTINUE},
            { "true",   TokenType.TRUE},
            { "false",  TokenType.FALSE},
            { "null",   TokenType.NULL},
            { "fun",    TokenType.FUNCTION},
            { "class",  TokenType.CLASS},
            { "this",  TokenType.THIS},
            { "super",  TokenType.SUPER},
            { ".",      TokenType.DOT},
            { "throw",  TokenType.THROW},
            { "test",  TokenType.TEST},
            { "testcase",  TokenType.TESTCASE},
            { "static",  TokenType.STATIC},
            { "yield",  TokenType.YIELD},
        };

        public static bool IsAlpha(int c)
            => (c >= 'a' && c <= 'z') ||
                (c >= 'A' && c <= 'Z') ||
                c == '_';

        public static bool IsAlphaNumber(int c) => IsAlpha(c) || NumberScannerTokenGenerator.IsDigit(c);

        public bool DoesMatchChar(ScannerBase scanner) => IsAlpha(scanner.CurrentChar);

        public void Consume(ScannerBase scanner)
        {
            workingSpaceStringBuilder.Clear();

            workingSpaceStringBuilder.Append(scanner.CurrentChar);

            while (IsAlphaNumber(scanner.Peek()))
            {
                scanner.Advance();
                workingSpaceStringBuilder.Append(scanner.CurrentChar);
            }

            var identString = workingSpaceStringBuilder.ToString();
            var token = TokenType.IDENTIFIER;

            if (keywords.TryGetValue(identString, out var keywordTokenType))
                token = keywordTokenType;

            scanner.AddToken(token, identString, identString);
        }
    }
}
