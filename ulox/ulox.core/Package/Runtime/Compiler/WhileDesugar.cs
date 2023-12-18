using System.Collections.Generic;

namespace ULox
{
    public class WhileDesugar : IDesugarStep
    {
        public Token ProcessReplace(Token currentToken, int currentTokenIndex, List<Token> tokens)
        {
            //we expect `while (exp)` and we are going to replace with `for(;exp;)`
            var returnToken = new Token(
                TokenType.FOR,
                currentToken.Lexeme,
                currentToken.Literal,
                currentToken.Line,
                currentToken.Character,
                currentToken.StringSourceIndex);

            tokens.Insert(currentTokenIndex+2, new Token(
                TokenType.END_STATEMENT,
                "",
                "",
                currentToken.Line,
                currentToken.Character,
                currentToken.StringSourceIndex));

            //find the end of exp
            var expEnd = ClosingParen(tokens, currentTokenIndex + 3);
            tokens.Insert(expEnd, new Token(
                TokenType.END_STATEMENT,
                "",
                "",
                currentToken.Line,
                currentToken.Character,
                currentToken.StringSourceIndex));

            return returnToken;
        }

        private static int ClosingParen(List<Token> tokens, int startingIndex)
        {
            var loc = startingIndex;
            var end = tokens.Count;
            var requiredClose = 1;
            while (loc < end)
            {
                var tok = tokens[loc];
                if (tok.TokenType == TokenType.OPEN_PAREN)
                    requiredClose++;
                else if (tok.TokenType == TokenType.CLOSE_PAREN)
                    requiredClose--;

                if (requiredClose <= 0)
                    return loc;

                loc++;
            }

            return loc;
        }

        public DesugarStepRequest RequestFromState(TokenIterator tokenIterator)
        {
            return tokenIterator.CurrentToken.TokenType == TokenType.WHILE 
                ? DesugarStepRequest.Replace 
                : DesugarStepRequest.None;
        }
    }
}
