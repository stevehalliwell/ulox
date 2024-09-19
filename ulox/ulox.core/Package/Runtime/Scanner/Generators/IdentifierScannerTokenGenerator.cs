using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace ULox
{
    public sealed class IdentifierScannerTokenGenerator : IScannerTokenGenerator
    {
        private readonly StringBuilder workingSpaceStringBuilder = new();
        private readonly Dictionary<string, TokenType> keywords = new();

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

            scanner.EmitToken(token, identString, identString);
        }
    }
}
