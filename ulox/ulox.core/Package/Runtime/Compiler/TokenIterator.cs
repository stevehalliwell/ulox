using System.Collections.Generic;

namespace ULox
{
    public sealed class TokenIterator
    {
        public Token CurrentToken { get; private set; }
        public Token PreviousToken { get; private set; }
        public string SourceName => _script.Name;

        private readonly Script _script;
        private readonly List<Token> _tokens;

        private int _currentTokenIndex = -1;

        public TokenIterator(Script script, List<Token> tokens)
        {
            _script = script;
            _tokens = tokens;
        }

        public string GetSourceSection(int start, int len)
        {
            return _script.Source.Substring(start, len);
        }

        public void Advance()
        {
            PreviousToken = CurrentToken;
            CurrentToken = _tokens[++_currentTokenIndex];
            if (CurrentToken.TokenType == TokenType.STRING)
            {
                var literalString = CurrentToken.Literal as string;
                var newStr = literalString;
                var locStart = InterpolationStart(literalString, 0);
                if (locStart != -1)
                {
                    var locEnd = InterpolationEnd(literalString, locStart + 1);
                    newStr = literalString.Substring(0, locStart);

                    var interpStr = literalString.Substring(locStart + 1, locEnd - locStart - 1);
                    var postInterpStr = literalString.Substring(locEnd + 1);
                    var scanner = new Scanner();
                    var interpTokens = scanner.Scan(new Script(_script.Name + "_interp", interpStr));
                    interpTokens.RemoveAt(interpTokens.Count-1);
                    interpTokens.Insert(0, new Token(
                        TokenType.OPEN_PAREN,
                        "(",
                        null,
                        CurrentToken.Line,
                        CurrentToken.Character, //tmp
                        CurrentToken.StringSourceIndex));//tmp
                    interpTokens.Insert(0, new Token(
                        TokenType.PLUS,
                        "+",
                        null,
                        CurrentToken.Line,
                        CurrentToken.Character, //tmp
                        CurrentToken.StringSourceIndex));//tmp
                    interpTokens.Add(new Token(
                       TokenType.CLOSE_PAREN,
                       ")",
                       null,
                       CurrentToken.Line,
                       CurrentToken.Character, //tmp
                       CurrentToken.StringSourceIndex));//tmp
                    interpTokens.Add(new Token(
                       TokenType.PLUS,
                       "+",
                       null,
                       CurrentToken.Line,
                       CurrentToken.Character, //tmp
                       CurrentToken.StringSourceIndex));//tmp
                    interpTokens.Add(new Token(
                       TokenType.STRING,
                       postInterpStr,
                       postInterpStr,
                       CurrentToken.Line,
                       CurrentToken.Character, //tmp
                       CurrentToken.StringSourceIndex));//tmp
                    _tokens.InsertRange(_currentTokenIndex + 1, interpTokens);
                }

                newStr = System.Text.RegularExpressions.Regex.Unescape(newStr);
                CurrentToken = new Token(
                    TokenType.STRING,
                    newStr,
                    newStr,
                    CurrentToken.Line,
                    CurrentToken.Character,
                    CurrentToken.StringSourceIndex);
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

        public bool MatchAny(params TokenType[] type)
        {
            for (int i = 0; i < type.Length; i++)
            {
                if (!Check(type[i])) continue;

                Advance();
                return true;
            }
            return false;
        }

        private static int InterpolationStart(string literalString, int startAt)
        {
            var loc = literalString.IndexOf('{', startAt);
            if (loc == -1)
                return -1;

            if (loc == 0)
                return loc;

            var prevChar = literalString[loc - 1];
            if (prevChar == '\\')
                return InterpolationStart(literalString, loc + 1);

            return loc;
        }

        private static int InterpolationEnd(string literalString, int loc)
        {
            var end = literalString.Length;
            var requiredClose = 1;
            while (loc < end)
            {
                var ch = literalString[loc];
                if (ch == '{')
                    requiredClose++;
                else if (ch == '}')
                    requiredClose--;
                
                if (requiredClose <= 0)
                    return loc;
                
                loc++;
            }

            return loc;
        }
    }
}
