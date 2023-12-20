using System.Collections.Generic;

namespace ULox
{
    public class WhileDesugar : IDesugarStep
    {
        public Token ProcessReplace(Token currentToken, int currentTokenIndex, List<Token> tokens)
        {
            //we expect `while (exp)` and we are going to replace with `for(;exp;)`
            var returnToken = currentToken.MutateType(TokenType.FOR);

            tokens.Insert(currentTokenIndex+2, currentToken.MutateType(TokenType.END_STATEMENT));

            //find the end of exp
            var expEnd = TokenIterator.FindClosing(tokens, currentTokenIndex + 3, TokenType.OPEN_PAREN, TokenType.CLOSE_PAREN);
            tokens.Insert(expEnd, currentToken.MutateType( TokenType.END_STATEMENT));

            return returnToken;
        }

        public DesugarStepRequest RequestFromState(TokenIterator tokenIterator)
        {
            return tokenIterator.CurrentToken.TokenType == TokenType.WHILE 
                ? DesugarStepRequest.Replace 
                : DesugarStepRequest.None;
        }
    }
}
