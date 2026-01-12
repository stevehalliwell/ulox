using System.Collections.Generic;

namespace ULox
{
    public class CompoundAssignDesugar : IDesugarStep
    {
        public void ProcessDesugar(int currentTokenIndex, List<Token> tokens, ICompilerDesugarContext context)
        {
            var currentToken = tokens[currentTokenIndex];
            var prevToken = tokens[currentTokenIndex - 1];

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

            tokens[currentTokenIndex] = currentToken.MutateType(TokenType.ASSIGN);

            tokens.InsertRange(currentTokenIndex + 1, new[] {
                prevToken,
                currentToken.MutateType(flatTokenType),});
        }

        public DesugarStepRequest IsDesugarRequested(TokenIterator tokenIterator, ICompilerDesugarContext context)
        {
            //we pretty dumb right now
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
