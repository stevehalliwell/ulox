using System.Collections.Generic;

namespace ULox
{
    public enum DesugarStepRequest
    {
        None,
        Replace,
    }

    public interface IDesugarStep
    {
        DesugarStepRequest RequestFromState(TokenIterator tokenIterator);
        Token ProcessReplace(Token currentToken, int currentTokenIndex, List<Token> tokens);
    }

    public sealed class TokenIterator
    {
        public Token CurrentToken { get; private set; }
        public Token PreviousToken { get; private set; }
        public string SourceName => _script.Name;

        private readonly Script _script;
        private readonly List<Token> _tokens;
        private readonly List<IDesugarStep> _desugarSteps = new List<IDesugarStep>();

        private int _currentTokenIndex = -1;

        public TokenIterator(Script script, List<Token> tokens)
        {
            _script = script;
            _tokens = tokens;

            _desugarSteps.Add(new StringInterpDesugar());
            _desugarSteps.Add(new WhileDesugar());
            _desugarSteps.Add(new CompoundAssignDesugar());
            _desugarSteps.Add(new LoopDesugar());
        }

        public string GetSourceSection(int start, int len)
        {
            return _script.Source.Substring(start, len);
        }

        public void Advance()
        {
            PreviousToken = CurrentToken;
            CurrentToken = _tokens[++_currentTokenIndex];

            foreach (var desugarStep in _desugarSteps)
            {
                var request = desugarStep.RequestFromState(this);
                switch (request)
                {
                case DesugarStepRequest.Replace:
                    CurrentToken = desugarStep.ProcessReplace(CurrentToken, _currentTokenIndex, _tokens);
                    break;
                case DesugarStepRequest.None:
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(request), request, null);
                }
            }
        }

        public void Consume(TokenType tokenType, string msg)
        {
            if (CurrentToken.TokenType == tokenType)
                Advance();
            else
                throw new CompilerException(msg, PreviousToken, $"source '{_script.Name}'");
        }

        public bool Check(TokenType type)
            => CurrentToken.TokenType == type;

        public bool Match(TokenType type)
        {
            if (!Check(type))
                return false;
            Advance();
            return true;
        }

        public TokenType PeekType(int ahead = 1)
        {
            if (CurrentToken.TokenType == TokenType.EOF
                || _currentTokenIndex + ahead >= _tokens.Count)
                return TokenType.EOF;
            return _tokens[_currentTokenIndex + ahead].TokenType;
        }

        public static int FindClosing(List<Token> tokens, int startingIndex, TokenType increaseType, TokenType decreaseType)
        {
            var loc = startingIndex;
            var end = tokens.Count;
            var requiredClose = 1;
            while (loc < end)
            {
                var tok = tokens[loc];
                if (tok.TokenType == increaseType)
                    requiredClose++;
                else if (tok.TokenType == decreaseType)
                    requiredClose--;

                if (requiredClose <= 0)
                    return loc;

                loc++;
            }

            return loc;
        }
    }
}
