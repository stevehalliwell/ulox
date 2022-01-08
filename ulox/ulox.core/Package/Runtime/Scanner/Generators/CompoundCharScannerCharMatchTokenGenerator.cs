using System.Linq;

namespace ULox
{
    public class CompoundCharScannerCharMatchTokenGenerator : IScannerTokenGenerator
    {
        private (char ch, TokenType regular, TokenType compound)[] CompoundMatches = new[]
        {
            ('+', TokenType.PLUS, TokenType.PLUS_EQUAL),
            ('-', TokenType.MINUS, TokenType.MINUS_EQUAL),
            ('*', TokenType.STAR, TokenType.STAR_EQUAL),
            ('%', TokenType.PERCENT, TokenType.PERCENT_EQUAL),
            ('!', TokenType.BANG, TokenType.BANG_EQUAL),
            ('=', TokenType.ASSIGN, TokenType.EQUALITY),
            ('<', TokenType.LESS, TokenType.LESS_EQUAL),
            ('>', TokenType.GREATER, TokenType.GREATER_EQUAL)
        };

        public void Consume(IScanner scanner)
        {
            var match = CompoundMatches.First(x => x.ch == scanner.CurrentChar);
            scanner.AddTokenSingle(scanner.Match('=') ? match.compound : match.regular);
        }

        public bool DoesMatchChar(char ch)
        {
            return CompoundMatches.Any(x => x.ch == ch);
        }
    }
}
