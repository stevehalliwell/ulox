using System.Collections.Generic;
using System.Text;

namespace ULox
{
    public class IdentifierScannerTokenGenerator : IScannerTokenGenerator
    {
        private readonly StringBuilder workingSpaceStringBuilder = new StringBuilder();

        private readonly Dictionary<string, TokenType> keywords = new Dictionary<string, TokenType>();

        public void Add(string name, TokenType tt) 
            => keywords[name] = tt;

        public void Add(params (string name, TokenType tt)[] toAdd)
        {
            foreach (var item in toAdd)
            {
                Add(item.name, item.tt);
            }
        }

        public static bool IsAlpha(int c)
            => (c >= 'a' && c <= 'z') ||
                (c >= 'A' && c <= 'Z') ||
                c == '_';

        public static bool IsAlphaNumber(int c) => IsAlpha(c) || NumberScannerTokenGenerator.IsDigit(c);

        public bool DoesMatchChar(char ch) => IsAlpha(ch);

        public void Consume(IScanner scanner)
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
