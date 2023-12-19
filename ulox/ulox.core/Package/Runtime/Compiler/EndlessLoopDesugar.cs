using System.Collections.Generic;

namespace ULox
{
    public class EndlessLoopDesugar : IDesugarStep
    {
        public Token ProcessReplace(Token currentToken, int currentTokenIndex, List<Token> tokens)
        {
            //we expect `loop {` and we are going to replace with `for(;true;)`
            var returnToken = new Token(
                TokenType.FOR,
                currentToken.Lexeme,
                currentToken.Literal,
                currentToken.Line,
                currentToken.Character,
                currentToken.StringSourceIndex);

            tokens.InsertRange(currentTokenIndex + 1, new[] {
                new Token(
                TokenType.OPEN_PAREN,
                "",
                "",
                currentToken.Line,
                currentToken.Character,
                currentToken.StringSourceIndex),
                new Token(
                TokenType.END_STATEMENT,
                "",
                "",
                currentToken.Line,
                currentToken.Character,
                currentToken.StringSourceIndex),
                new Token(
                TokenType.END_STATEMENT,
                "",
                "",
                currentToken.Line,
                currentToken.Character,
                currentToken.StringSourceIndex),
                new Token(
                TokenType.CLOSE_PAREN,
                "",
                "",
                currentToken.Line,
                currentToken.Character,
                currentToken.StringSourceIndex),});


            return returnToken;
        }

        public DesugarStepRequest RequestFromState(TokenIterator tokenIterator)
        {
            return tokenIterator.CurrentToken.TokenType == TokenType.LOOP && tokenIterator.PeekType() == TokenType.OPEN_BRACE
                ? DesugarStepRequest.Replace
                : DesugarStepRequest.None;
        }
    }
}
