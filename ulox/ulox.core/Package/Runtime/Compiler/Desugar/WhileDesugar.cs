using System.Collections.Generic;

namespace ULox
{
    public class WhileDesugar : IDesugarStep
    {
        public void ProcessDesugar(int currentTokenIndex, List<Token> tokens)
        {
            //we expect `while (exp)` and we are going to replace with `for(;exp;)`
            var currentToken = tokens[currentTokenIndex];
            var returnToken = currentToken.MutateType(TokenType.FOR);

            tokens.Insert(currentTokenIndex+2, currentToken.MutateType(TokenType.END_STATEMENT));

            //find the end of exp
            var expEnd = TokenIterator.FindClosing(tokens, currentTokenIndex + 3, TokenType.OPEN_PAREN, TokenType.CLOSE_PAREN);
            tokens.Insert(expEnd, currentToken.MutateType( TokenType.END_STATEMENT));

            tokens[currentTokenIndex] = returnToken;
        }

        public DesugarStepRequest IsDesugarRequested(TokenIterator tokenIterator)
        {
            return tokenIterator.CurrentToken.TokenType == TokenType.WHILE 
                ? DesugarStepRequest.Replace 
                : DesugarStepRequest.None;
        }
    }
}
