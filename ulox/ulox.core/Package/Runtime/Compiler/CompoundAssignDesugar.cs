using System.Collections.Generic;

namespace ULox
{
    public class CompoundAssignDesugar : IDesugarStep
    {
        public Token ProcessReplace(Token currentToken, int currentTokenIndex, List<Token> tokens)
        {
            var prevToken = tokens[currentTokenIndex - 1];
            //we expect `loop {` and we are going to replace with `for(;true;)`
            var returnToken = new Token(
                TokenType.ASSIGN,
                currentToken.Lexeme,
                currentToken.Literal,
                currentToken.Line,
                currentToken.Character,
                currentToken.StringSourceIndex);

            var flatTokenType = currentToken.TokenType;
            switch (currentToken.TokenType)
            {
            case TokenType.PLUS_EQUAL:
                flatTokenType = TokenType.PLUS;
                break;
            case TokenType.MINUS_EQUAL:
                flatTokenType = TokenType.MINUS;
                break;
            case TokenType.STAR_EQUAL:
                flatTokenType = TokenType.STAR;
                break;
            case TokenType.SLASH_EQUAL:
                flatTokenType = TokenType.SLASH;
                break;
            case TokenType.PERCENT_EQUAL:
                flatTokenType = TokenType.PERCENT;
                break;
            }

            tokens.InsertRange(currentTokenIndex + 1, new[] {
                prevToken,
                new Token(
                flatTokenType,
                "",
                "",
                currentToken.Line,
                currentToken.Character,
                currentToken.StringSourceIndex),});


            return returnToken;
        }

        public DesugarStepRequest RequestFromState(TokenIterator tokenIterator)
        {
            //we pretty dump right now
            if(tokenIterator.PreviousToken.TokenType != TokenType.IDENTIFIER)
                return DesugarStepRequest.None;

            var tok = tokenIterator.CurrentToken.TokenType;
            return tok == TokenType.PLUS_EQUAL
                || tok == TokenType.MINUS_EQUAL
                || tok == TokenType.STAR_EQUAL
                || tok == TokenType.SLASH_EQUAL
                || tok == TokenType.PERCENT_EQUAL
                ? DesugarStepRequest.Replace
                : DesugarStepRequest.None;
        }
    }
}
