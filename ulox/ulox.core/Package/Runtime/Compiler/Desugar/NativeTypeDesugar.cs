using System.Collections.Generic;

namespace ULox
{
    public class NativeTypeDesugar : IDesugarStep
    {
        private readonly string _nativeTypeName;
        private readonly TokenType[] _sequenceMatch;

        public NativeTypeDesugar(
            string nativeTypeName,
            TokenType[] sequenceMatch)
        {
            _nativeTypeName = nativeTypeName;
            _sequenceMatch = sequenceMatch;
        }

        public void ProcessDesugar(int currentTokenIndex, List<Token> tokens, ICompilerDesugarContext context)
        {
            var currentToken = tokens[currentTokenIndex];
            var returnToken = currentToken.Mutate(TokenType.IDENTIFIER, _nativeTypeName);
            var tokensToRemove = _sequenceMatch.Length-1;

            tokens.RemoveRange(currentTokenIndex + 1, tokensToRemove);

            tokens.InsertRange(currentTokenIndex + 1, new[]{
                currentToken.MutateType(TokenType.OPEN_PAREN),
                currentToken.MutateType(TokenType.CLOSE_PAREN),
                });

            tokens[currentTokenIndex] = returnToken;
        }

        public DesugarStepRequest IsDesugarRequested(TokenIterator tokenIterator, ICompilerDesugarContext context)
        {
            var currentToken = tokenIterator.CurrentToken;
            if (currentToken.TokenType != _sequenceMatch[0])
                return DesugarStepRequest.None;

            for (var i = 1; i < _sequenceMatch.Length; i++)
            {
                if (tokenIterator.PeekType(i) != _sequenceMatch[i])
                    return DesugarStepRequest.None;
            }

            return DesugarStepRequest.Replace;
        }
    }
}
