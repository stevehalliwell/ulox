using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class IdentifierScannerTokenGenerator : IScannerTokenGenerator
    {
        private readonly Dictionary<string, TokenType> keywords = new();
        private int _startingIndex;

        public void Add(string name, TokenType tt) 
            => keywords[name] = tt;

        public void Add(params (string name, TokenType tt)[] toAdd)
        {
            foreach (var (name, tt) in toAdd)
            {
                Add(name, tt);
            }
        }

        public static bool IsAlpha(int c)
            => (c >= 'a' && c <= 'z') ||
                (c >= 'A' && c <= 'Z') ||
                c == '_';

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAlphaNumber(int c) => IsAlpha(c) || NumberScannerTokenGenerator.IsDigit(c);

        public bool DoesMatchChar(char ch) => IsAlpha(ch);

        public void Consume(Scanner scanner)
        {
            _startingIndex = scanner.CurrentIndex;

            while (IsAlphaNumber(scanner.Peek()))
            {
                scanner.Advance();
            }

            var identString = scanner.SubStrFrom(_startingIndex);
            var token = TokenType.IDENTIFIER;

            if (keywords.TryGetValue(identString, out var keywordTokenType))
                token = keywordTokenType;

            scanner.EmitToken(token, identString);
        }
    }
}
