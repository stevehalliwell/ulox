using System.Collections.Generic;

namespace ULox
{
    public class ClassInitArgMatchDesugar : IDesugarStep
    {
        public DesugarStepRequest IsDesugarRequested(TokenIterator tokenIterator, ICompilerDesugarContext context)
        {
            if (tokenIterator.CurrentToken.TokenType == TokenType.INIT
                && context.IsInClass()
                && tokenIterator.PeekType(1) == TokenType.OPEN_PAREN
                && tokenIterator.PeekType(2) == TokenType.IDENTIFIER)
                return DesugarStepRequest.Replace;
            // if currenlty compiling a class and method name `init(`
            return DesugarStepRequest.None;
        }

        public void ProcessDesugar(int currentTokenIndex, List<Token> tokens, ICompilerDesugarContext context)
        {
            //grab all from `init(` till `)`
            var end = TokenIterator.FindClosing(tokens, currentTokenIndex + 2, TokenType.OPEN_PAREN, TokenType.CLOSE_PAREN);
            var insertTokens = new List<Token>();
            for (var i = currentTokenIndex + 2; i < end; i++)
            {
                var arg = tokens[i];
                if (arg.TokenType != TokenType.IDENTIFIER
                    || !context.DoesCurrentClassHaveMatchingField(arg.Literal))
                    continue;

                insertTokens.Add(arg.MutateType(TokenType.THIS));
                insertTokens.Add(arg.MutateType(TokenType.DOT));
                insertTokens.Add(arg);
                insertTokens.Add(arg.MutateType(TokenType.ASSIGN));
                insertTokens.Add(arg);
                insertTokens.Add(arg.MutateType(TokenType.END_STATEMENT));
            }

            tokens.InsertRange(end + 2, insertTokens);
        }
    }
}
