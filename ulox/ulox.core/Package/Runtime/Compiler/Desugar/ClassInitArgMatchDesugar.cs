using System.Collections.Generic;
using System.Linq;

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
            var initArgs = tokens.GetRange(currentTokenIndex + 2, end - currentTokenIndex - 2);
            
            //filter matching name in class fields
            var argnames = initArgs
                .Where(x => x.TokenType == TokenType.IDENTIFIER)
                .Where(x => context.DoesCurrentClassHaveMatchingField(x.Literal));

            //insert `this.` before each field name and assign through
            tokens.InsertRange(end + 2, argnames.SelectMany(x => new[]
                {
                    x.MutateType(TokenType.THIS),
                    x.MutateType(TokenType.DOT),
                    x,
                    x.MutateType(TokenType.ASSIGN),
                    x,
                    x.MutateType(TokenType.END_STATEMENT),
                }));
        }
    }
}
