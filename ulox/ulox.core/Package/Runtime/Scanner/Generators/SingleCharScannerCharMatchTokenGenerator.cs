using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class SingleCharScannerCharMatchTokenGenerator : IScannerTokenGenerator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Token Consume(Scanner scanner)
        {
            switch (scanner.CurrentChar)
            {
            case '(':
                return scanner.EmitTokenSingle(TokenType.OPEN_PAREN);
            case ')':
                return scanner.EmitTokenSingle(TokenType.CLOSE_PAREN);
            case '{':
                return scanner.EmitTokenSingle(TokenType.OPEN_BRACE);
            case '}':
                return scanner.EmitTokenSingle(TokenType.CLOSE_BRACE);
            case '[':
                return scanner.EmitTokenSingle(TokenType.OPEN_BRACKET);
            case ']':
                return scanner.EmitTokenSingle(TokenType.CLOSE_BRACKET);
            case ',':
                return scanner.EmitTokenSingle(TokenType.COMMA);
            case ';':
                return scanner.EmitTokenSingle(TokenType.END_STATEMENT);
            case '.':
                return scanner.EmitTokenSingle(TokenType.DOT);
            case ':':
                return scanner.EmitTokenSingle(TokenType.COLON);
            case '?':
                return scanner.EmitTokenSingle(TokenType.QUESTION);
            default:
                return scanner.SharedNoToken;

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
