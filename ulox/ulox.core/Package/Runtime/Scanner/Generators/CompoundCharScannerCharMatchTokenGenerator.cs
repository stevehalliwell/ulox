namespace ULox
{
    public sealed class CompoundCharScannerCharMatchTokenGenerator : IScannerTokenGenerator
    {
        public Token Consume(Scanner scanner)
        {
            switch (scanner.CurrentChar)
            {
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
            default:
                return Scanner.SharedNoToken;
            }
        }

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
