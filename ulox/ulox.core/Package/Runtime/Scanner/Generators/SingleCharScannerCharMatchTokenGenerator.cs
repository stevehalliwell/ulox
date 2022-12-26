using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class SingleCharScannerCharMatchTokenGenerator : IScannerTokenGenerator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Consume(Scanner scanner)
        {
            switch (scanner.CurrentChar)
            {
            case '(':
                scanner.AddTokenSingle(TokenType.OPEN_PAREN);
                break;
            case ')':
                scanner.AddTokenSingle(TokenType.CLOSE_PAREN);
                break;
            case '{':
                scanner.AddTokenSingle(TokenType.OPEN_BRACE);
                break;
            case '}':
                scanner.AddTokenSingle(TokenType.CLOSE_BRACE);
                break;
            case '[':
                scanner.AddTokenSingle(TokenType.OPEN_BRACKET);
                break;
            case ']':
                scanner.AddTokenSingle(TokenType.CLOSE_BRACKET);
                break;
            case ',':
                scanner.AddTokenSingle(TokenType.COMMA);
                break;
            case ';':
                scanner.AddTokenSingle(TokenType.END_STATEMENT);
                break;
            case '.':
                scanner.AddTokenSingle(TokenType.DOT);
                break;
            case ':':
                scanner.AddTokenSingle(TokenType.COLON);
                break;
            case '?':
                scanner.AddTokenSingle(TokenType.QUESTION);
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
            case '(':
            case ')':
            case '{':
            case '}':
            case '[':
            case ']':
            case ',':
            case ';':
            case '.':
            case ':':
            case '?':
                return true;
            default:
                return false;
            }
        }
    }
}
