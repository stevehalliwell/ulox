using System.Collections.Generic;

namespace ULox
{
    //TODO: we don' need types for this, just make the base configurable
    public abstract class NativeTypeDesugar : IDesugarStep
    {
        private readonly string _nativeTypeName;
        private readonly int _tokenRemoveCount;
        private readonly TokenType[] _sequnceMatch;

        public NativeTypeDesugar(
            string nativeTypeName,
            int tokenRemoveCount,
            TokenType[] sequnceMatch)
        {
            _nativeTypeName = nativeTypeName;
            _tokenRemoveCount = tokenRemoveCount;
            _sequnceMatch = sequnceMatch;
        }

        public void ProcessDesugar(int currentTokenIndex, List<Token> tokens, ICompilerDesugarContext context)
        {
            var currentToken = tokens[currentTokenIndex];
            var returnToken = currentToken.Mutate(TokenType.IDENTIFIER, _nativeTypeName);

            tokens.RemoveRange(currentTokenIndex + 1, _tokenRemoveCount);

            tokens.InsertRange(currentTokenIndex + 1, new[]{
                currentToken.MutateType(TokenType.OPEN_PAREN),
                currentToken.MutateType(TokenType.CLOSE_PAREN),
                });

            tokens[currentTokenIndex] = returnToken;
        }

        public DesugarStepRequest IsDesugarRequested(TokenIterator tokenIterator, ICompilerDesugarContext context)
        {
            var currentToken = tokenIterator.CurrentToken;
            if (currentToken.TokenType != _sequnceMatch[0])
                return DesugarStepRequest.None;

            for (var i = 1; i < _sequnceMatch.Length; i++)
            {
                if (tokenIterator.PeekType(i) != _sequnceMatch[i])
                    return DesugarStepRequest.None;
            }

            return DesugarStepRequest.Replace;
        }
    }

    public class ListDesugar : NativeTypeDesugar
    {
        public ListDesugar() : base("List", 1, new[] { TokenType.OPEN_BRACKET, TokenType.CLOSE_BRACKET })
        { }
    }

    public class MapDesugar : NativeTypeDesugar
    {
        public MapDesugar() : base("Map", 2, new[] { TokenType.OPEN_BRACKET, TokenType.COLON, TokenType.CLOSE_BRACKET })
        { }
    }

    public class DynamicDesugar : NativeTypeDesugar
    {
        public DynamicDesugar() : base("Dynamic", 2, new[] { TokenType.OPEN_BRACE, TokenType.ASSIGN, TokenType.CLOSE_BRACE})
        { }
    }
}
