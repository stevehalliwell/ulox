using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class DirectSymbolScannerMatchTokenGenerator : IScannerTokenGenerator
    {
        public void Consume(Scanner scanner)
        {
            switch (scanner.CurrentChar)
            {
            case '(':
                scanner.EmitTokenSingle(TokenType.OPEN_PAREN);
                break;
            case ')':
                scanner.EmitTokenSingle(TokenType.CLOSE_PAREN);
                break;
            case '{':
                scanner.EmitTokenSingle(TokenType.OPEN_BRACE);
                break;
            case '}':
                scanner.EmitTokenSingle(TokenType.CLOSE_BRACE);
                break;
            case '[':
                scanner.EmitTokenSingle(TokenType.OPEN_BRACKET);
                break;
            case ']':
                scanner.EmitTokenSingle(TokenType.CLOSE_BRACKET);
                break;
            case ',':
                scanner.EmitTokenSingle(TokenType.COMMA);
                break;
            case ';':
                scanner.EmitTokenSingle(TokenType.END_STATEMENT);
                break;
            case '.':
                scanner.EmitTokenSingle(TokenType.DOT);
                break;
            case ':':
                scanner.EmitTokenSingle(TokenType.COLON);
                break;
            case '?':
                scanner.EmitTokenSingle(TokenType.QUESTION);
                break;
                //compound
            case '+':
                scanner.EmitTokenSingle(!scanner.Match('=') ? TokenType.PLUS : TokenType.PLUS_EQUAL);
                break;
            case '-':
                scanner.EmitTokenSingle(!scanner.Match('=') ? TokenType.MINUS : TokenType.MINUS_EQUAL);
                break;
            case '*':
                scanner.EmitTokenSingle(!scanner.Match('=') ? TokenType.STAR : TokenType.STAR_EQUAL);
                break;
            case '%':
                scanner.EmitTokenSingle(!scanner.Match('=') ? TokenType.PERCENT : TokenType.PERCENT_EQUAL);
                break;
            case '!':
                scanner.EmitTokenSingle(!scanner.Match('=') ? TokenType.BANG : TokenType.BANG_EQUAL);
                break;
            case '=':
                scanner.EmitTokenSingle(!scanner.Match('=') ? TokenType.ASSIGN : TokenType.EQUALITY);
                break;
            case '<':
                scanner.EmitTokenSingle(!scanner.Match('=') ? TokenType.LESS : TokenType.LESS_EQUAL);
                break;
            case '>':
                scanner.EmitTokenSingle(!scanner.Match('=') ? TokenType.GREATER : TokenType.GREATER_EQUAL);
                break;
            case '/':
                ConsumeSlash(scanner);
                break;
                //whitespace
            case ' ':
            case '\r':
            case '\t':
            case '\n':
            default:
                break;
            }
        }

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
                //potential compound
            case '+':
            case '-':
            case '*':
            case '%':
            case '!':
            case '=':
            case '<':
            case '>':
            //slash
            case '/':
            //whitespace
            case ' ':
            case '\r':
            case '\t':
            case '\n':
                return true;
            default:
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ConsumeSlash(Scanner scanner)
        {
            if (scanner.Match('/'))
            {
                scanner.ReadLine();
            }
            else if (scanner.Match('*'))
            {
                ConsumeBlockComment(scanner);
            }
            else
            {
                scanner.EmitTokenSingle(scanner.Match('=') ? TokenType.SLASH_EQUAL : TokenType.SLASH);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ConsumeBlockComment(Scanner scanner)
        {
            while (!scanner.IsAtEnd())
            {
                if (scanner.Match('*') && scanner.Match('/'))
                {
                    break;
                }
                else
                {
                    scanner.Advance();
                }
            }
        }
    }
}
