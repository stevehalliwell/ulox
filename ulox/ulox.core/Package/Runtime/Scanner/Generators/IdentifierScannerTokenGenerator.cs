using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace ULox
{
    public sealed class IdentifierScannerTokenGenerator : IScannerTokenGenerator
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAlpha(int c)
            => (c >= 'a' && c <= 'z') ||
                (c >= 'A' && c <= 'Z') ||
                c == '_';

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAlphaNumber(int c) => IsAlpha(c) || NumberScannerTokenGenerator.IsDigit(c);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool DoesMatchChar(char ch) => IsAlpha(ch);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
