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
        bool DoesCurrentClassHaveMatchingField(string x);
        bool IsInClass();
        string UniqueLocalName(string prefix);
        TypeInfo TypeInfo { get; }
    }

    public interface IDesugarStep
    {
        DesugarStepRequest IsDesugarRequested(TokenIterator tokenIterator, ICompilerDesugarContext context);
        void ProcessDesugar(int currentTokenIndex, List<Token> tokens, ICompilerDesugarContext context);
    }

    //todo: split desugar that needs context from that which doesn't, and blast through all the 
    //  desugars that don't need context ahead of time. If the desugar doesn't need to read ahead,
    //  it can be done at scan token emit.

    public sealed class TokenIterator
    {
        public Token CurrentToken { get; private set; }
        public Token PreviousToken { get; private set; }
        public int PreviousTokenLine { get; private set; }
        public int PreviousTokenCharacter { get; private set; }
        public string SourceName => _tokeisedScript.SourceScript.Name;

        private readonly Compiler _compiler;
        private readonly TokenisedScript _tokeisedScript;
        private readonly ICompilerDesugarContext _compilerDesugarContext;
        private readonly StringInterpDesugar _stringInterpDesugar = new();
        private readonly CompoundAssignDesugar _compoundAssignDesugar = new();
        private readonly LoopDesugar _loopDesugar = new();
        private readonly ClassInitArgMatchDesugar _classInitArgMatchDesugar = new();

        private int _currentTokenIndex = -1;
        private int _currentTokenLine;
        private int _currentTokenCharacter;
        private int _lineIndex;
        private int _lineStartSourceIndex;
        private int _lineEndSourceIndex;

        public TokenIterator(
            TokenisedScript tokenisedScript,
            ICompilerDesugarContext compilerDesugarContext,
            Compiler compiler)
        {
            _compiler = compiler;
            _tokeisedScript = tokenisedScript;
            _compilerDesugarContext = compilerDesugarContext;
            _lineEndSourceIndex = tokenisedScript.LineLengths.Length > 0 ? tokenisedScript.LineLengths[0] : 0;
        }

        public string GetSourceSection(int start, int len)
        {
            return _tokeisedScript.SourceScript.Source.Substring(start, len);
        }

        public void Advance()
        {
            PreviousToken = CurrentToken;
            PreviousTokenLine = _currentTokenLine;
            PreviousTokenCharacter = _currentTokenCharacter;

            CurrentToken = _tokeisedScript.Tokens[++_currentTokenIndex];
            (_currentTokenLine, _currentTokenCharacter) = GetCachedLineAndCharacter(CurrentToken.StringSourceIndex);

            switch (CurrentToken.TokenType)
            {
            case TokenType.STRING:
                TryDesugar(_stringInterpDesugar);
                break;
            case TokenType.PLUS_EQUAL:
            case TokenType.MINUS_EQUAL:
            case TokenType.STAR_EQUAL:
            case TokenType.SLASH_EQUAL:
            case TokenType.PERCENT_EQUAL:
                TryDesugar(_compoundAssignDesugar);
                break;
            case TokenType.LOOP:
                TryDesugar(_loopDesugar);
                break;
            case TokenType.INIT:
                TryDesugar(_classInitArgMatchDesugar);
                break;
            }
        }

        private void TryDesugar(IDesugarStep desugarStep)
        {
            var request = desugarStep.IsDesugarRequested(this, _compilerDesugarContext);
            switch (request)
            {
            case DesugarStepRequest.Replace:
                desugarStep.ProcessDesugar(_currentTokenIndex, _tokeisedScript.Tokens, _compilerDesugarContext);
                CurrentToken = _tokeisedScript.Tokens[_currentTokenIndex];
                break;
            case DesugarStepRequest.None:
                break;
            default:
                throw new System.ArgumentOutOfRangeException(nameof(request), request, null);
            }
        }

        public void Consume(TokenType tokenType, string msg)
        {
            if (CurrentToken.TokenType == tokenType)
                Advance();
            else
                _compiler.ThrowCompilerException(msg);
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
            if (index < 0 || index >= _tokeisedScript.Tokens.Count)
                return TokenType.EOF;
            return _tokeisedScript.Tokens[index].TokenType;
        }

        public (int line, int chararacter) GetLineAndCharacter(int stringSourceIndex)
        {
            var lineLengths = _tokeisedScript.LineLengths;
            var len = lineLengths.Length;
            for (int i = 0; i < len; i++)
            {
                stringSourceIndex -= lineLengths[i];
                if (stringSourceIndex <= 0)
                    return (i + 1, lineLengths[i] + stringSourceIndex + 1);
            }
            return (len+1, lineLengths[len-1]+1);
        }

        private (int line, int character) GetCachedLineAndCharacter(int stringSourceIndex)
        {
            var lineLengths = _tokeisedScript.LineLengths;
            var len = lineLengths.Length;
            if (len == 0)
                return (1, 1);

            if (stringSourceIndex < _lineStartSourceIndex)
                return GetLineAndCharacter(stringSourceIndex);

            while (_lineIndex < len - 1 && stringSourceIndex > _lineEndSourceIndex)
            {
                _lineStartSourceIndex = _lineEndSourceIndex;
                _lineEndSourceIndex += lineLengths[++_lineIndex];
            }

            if (stringSourceIndex <= _lineEndSourceIndex)
                return (_lineIndex + 1, stringSourceIndex - _lineStartSourceIndex + 1);

            return (len + 1, lineLengths[len - 1] + 1);
        }
    }
}