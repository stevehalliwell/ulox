using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class DirectSymbolScannerMatchTokenGenerator : IScannerTokenGenerator
    {
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
                //compound
            case '+':
                return scanner.EmitTokenSingle(!scanner.Match('=') ? TokenType.PLUS : TokenType.PLUS_EQUAL);
            case '-':
                return scanner.EmitTokenSingle(!scanner.Match('=') ? TokenType.MINUS : TokenType.MINUS_EQUAL);
            case '*':
                return scanner.EmitTokenSingle(!scanner.Match('=') ? TokenType.STAR : TokenType.STAR_EQUAL);
            case '%':
                return scanner.EmitTokenSingle(!scanner.Match('=') ? TokenType.PERCENT : TokenType.PERCENT_EQUAL);
            case '!':
                return scanner.EmitTokenSingle(!scanner.Match('=') ? TokenType.BANG : TokenType.BANG_EQUAL);
            case '=':
                return scanner.EmitTokenSingle(!scanner.Match('=') ? TokenType.ASSIGN : TokenType.EQUALITY);
            case '<':
                return scanner.EmitTokenSingle(!scanner.Match('=') ? TokenType.LESS : TokenType.LESS_EQUAL);
            case '>':
                return scanner.EmitTokenSingle(!scanner.Match('=') ? TokenType.GREATER : TokenType.GREATER_EQUAL);
            case '/':
                return ConsumeSlash(scanner);
                //whitespace
            case ' ':
            case '\r':
            case '\t':
            case '\n':
            default:
                return Scanner.SharedNoToken;
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
        private Token ConsumeSlash(Scanner scanner)
        {
            if (scanner.Match('/'))
            {
                scanner.ReadLine();
                return Scanner.SharedNoToken;
            }
            else if (scanner.Match('*'))
            {
                return ConsumeBlockComment(scanner);
            }
            else
            {
                return scanner.EmitTokenSingle(scanner.Match('=') ? TokenType.SLASH_EQUAL : TokenType.SLASH);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Token ConsumeBlockComment(Scanner scanner)
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
            return Scanner.SharedNoToken;
        }
    }
}
