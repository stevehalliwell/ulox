using System.Collections.Generic;

namespace ULox
{
    public enum DesugarStepRequest
    {
        None,
        Replace,
    }

    public interface ICompilerDesugarContext
    {
        bool DoesClassHaveMatchingField(string x);
        bool IsInClass();
    }

    public interface IDesugarStep
    {
        DesugarStepRequest IsDesugarRequested(TokenIterator tokenIterator, ICompilerDesugarContext context);
        void ProcessDesugar(int currentTokenIndex, List<Token> tokens, ICompilerDesugarContext context);
    }

    public sealed class TokenIterator
    {
        public Token CurrentToken { get; private set; }
        public Token PreviousToken { get; private set; }
        public string SourceName => _script.Name;

        private readonly Script _script;
        private readonly List<Token> _tokens;
        private readonly ICompilerDesugarContext _compilerDesugarContext;
        private readonly List<IDesugarStep> _desugarSteps = new List<IDesugarStep>();

        private int _currentTokenIndex = -1;

        public TokenIterator(
            Script script,
            List<Token> tokens,
            ICompilerDesugarContext compilerDesugarContext)
        {
            _script = script;
            _tokens = tokens;
            _compilerDesugarContext = compilerDesugarContext;

            _desugarSteps.Add(new StringInterpDesugar());
            _desugarSteps.Add(new WhileDesugar());
            _desugarSteps.Add(new CompoundAssignDesugar());
            _desugarSteps.Add(new LoopDesugar());
            _desugarSteps.Add(new ListDesugar());
            _desugarSteps.Add(new MapDesugar());
            _desugarSteps.Add(new DynamicDesugar());
            _desugarSteps.Add(new ClassInitArgMatchDesugar());
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
                var request = desugarStep.IsDesugarRequested(this, _compilerDesugarContext);
                switch (request)
                {
                case DesugarStepRequest.Replace:
                    desugarStep.ProcessDesugar(_currentTokenIndex, _tokens, _compilerDesugarContext);
                    CurrentToken = _tokens[_currentTokenIndex];
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

        public TokenType PeekType(int v)
        {
            var index = _currentTokenIndex + v;
            if (index < 0 || index >= _tokens.Count)
                return TokenType.EOF;
            return _tokens[index].TokenType;
        }
    }
}
