using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class CompoundCharScannerCharMatchTokenGenerator : IScannerTokenGenerator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Consume(IScanner scanner)
        {
            switch (scanner.CurrentChar)
            {
            case '+':
                scanner.AddTokenSingle(!scanner.Match('=') ? TokenType.PLUS : TokenType.PLUS_EQUAL);
                break;
            case '-':
                scanner.AddTokenSingle(!scanner.Match('=') ? TokenType.MINUS : TokenType.MINUS_EQUAL);
                break;
            case '*':
                scanner.AddTokenSingle(!scanner.Match('=') ? TokenType.STAR : TokenType.STAR_EQUAL);
                break;
            case '%':
                scanner.AddTokenSingle(!scanner.Match('=') ? TokenType.PERCENT : TokenType.PERCENT_EQUAL);
                break;
            case '!':
                scanner.AddTokenSingle(!scanner.Match('=') ? TokenType.BANG : TokenType.BANG_EQUAL);
                break;
            case '=':
                scanner.AddTokenSingle(!scanner.Match('=') ? TokenType.ASSIGN : TokenType.EQUALITY);
                break;
            case '<':
                scanner.AddTokenSingle(!scanner.Match('=') ? TokenType.LESS : TokenType.LESS_EQUAL);
                break;
            case '>':
                scanner.AddTokenSingle(!scanner.Match('=') ? TokenType.GREATER : TokenType.GREATER_EQUAL);
                break;
            default:
                //throw
                break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool DoesMatchChar(char ch)
        {
            switch (ch)
            {
            case '+':
            case '-':
            case '*':
            case '%':
            case '!':
            case '=':
            case '<':
            case '>':
                return true;
            default:
                return false;
            }
        }
    }
}
