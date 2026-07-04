using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace ULox
{
    public class ScannerException : UloxException
    {
        public ScannerException(string msg, TokenType tokenType, int line, int character, string location)
            : base($"{msg} got {tokenType} in {location} at {line}:{character}") { }
    }

    public sealed class Scanner
    {
        public const int TokenStartingCapacity = 500;
        private string _source;
        private int _index;
        private int _previousRunningCount;
        private readonly List<int> _lineLengths = new();
        public char CurrentChar { get; private set; }
        public int CurrentIndex => _index;
        private Script _script;
        private List<Token> _tokens;
        private readonly StringBuilder _workingSpaceStringBuilder = new();

        public Scanner()
        {
            Reset();
        }

        public void Reset()
        {
            _source = null;
            _index = -1;
            _previousRunningCount = 0;
            _lineLengths.Clear();
            _script = default;
            _workingSpaceStringBuilder.Clear();
        }

        public TokenisedScript Scan(Script script)
        {
            _script = script;
            _source = _script.Source;
            _index = -1;
            _previousRunningCount = 0;
            _lineLengths.Clear();
            _tokens = new List<Token>(TokenStartingCapacity);

            while (!IsAtEnd())
            {
                Advance();
                Consume();
            }

            _lineLengths.Add(_index - _previousRunningCount);
            EmitTokenSingle(TokenType.EOF);

            return new TokenisedScript(_tokens, _lineLengths.ToArray(), script);
        }

        private void Consume()
        {
            var ch = CurrentChar;
            if (ConsumeDirectSymbol())
                return;
            if (IsDigit(ch))
            {
                ConsumeDigit();
                return;
            }
            if (IsAlpha(ch))
            {
                ConsumeIdent();
                return;
            }

            ThrowScannerException($"Unexpected character '{ch}'");
        }

        private void ConsumeString()
        {
            var str = default(string);
            _workingSpaceStringBuilder.Clear();
            var prevChar = CurrentChar;
            Advance();//skip leading "
            while (!IsAtEnd())
            {
                if (CurrentChar == '"'
                    && prevChar != '\\')
                {
                    str = _workingSpaceStringBuilder.ToString();
                    EmitToken(TokenType.STRING, str);
                    return;
                }

                _workingSpaceStringBuilder.Append(CurrentChar);

                prevChar = CurrentChar;
                Advance();
            }

            //we don't want this but when doing expression only mode the last char and the close of quote can be the same
            if (CurrentChar != '"')
                ThrowScannerException("Unterminated String");

            str = _workingSpaceStringBuilder.ToString();
            EmitToken(TokenType.STRING, str);
        }

        private bool ConsumeDirectSymbol()
        {
            switch (CurrentChar)
            {
                case '"':
                    ConsumeString();
                    break;
                case '(':
                    EmitTokenSingle(TokenType.OPEN_PAREN);
                    break;
                case ')':
                    EmitTokenSingle(TokenType.CLOSE_PAREN);
                    break;
                case '{':
                    EmitTokenSingle(TokenType.OPEN_BRACE);
                    break;
                case '}':
                    EmitTokenSingle(TokenType.CLOSE_BRACE);
                    break;
                case '[':
                    EmitTokenSingle(TokenType.OPEN_BRACKET);
                    break;
                case ']':
                    EmitTokenSingle(TokenType.CLOSE_BRACKET);
                    break;
                case ',':
                    EmitTokenSingle(TokenType.COMMA);
                    break;
                case ';':
                    EmitTokenSingle(TokenType.END_STATEMENT);
                    break;
                case '.':
                    EmitTokenSingle(TokenType.DOT);
                    break;
                case ':':
                    EmitTokenSingle(TokenType.COLON);
                    break;
                //compound
                case '+':
                    EmitTokenSingle(!Match('=') ? TokenType.PLUS : TokenType.PLUS_EQUAL);
                    break;
                case '-':
                    EmitTokenSingle(!Match('=') ? TokenType.MINUS : TokenType.MINUS_EQUAL);
                    break;
                case '*':
                    EmitTokenSingle(!Match('=') ? TokenType.STAR : TokenType.STAR_EQUAL);
                    break;
                case '%':
                    EmitTokenSingle(!Match('=') ? TokenType.PERCENT : TokenType.PERCENT_EQUAL);
                    break;
                case '!':
                    EmitTokenSingle(!Match('=') ? TokenType.BANG : TokenType.BANG_EQUAL);
                    break;
                case '=':
                    EmitTokenSingle(!Match('=') ? TokenType.ASSIGN : TokenType.EQUALITY);
                    break;
                case '<':
                    EmitTokenSingle(!Match('=') ? TokenType.LESS : TokenType.LESS_EQUAL);
                    break;
                case '>':
                    EmitTokenSingle(!Match('=') ? TokenType.GREATER : TokenType.GREATER_EQUAL);
                    break;
                case '/':
                    ConsumeSlash();
                    break;
                //whitespace
                case ' ':
                case '\r':
                case '\t':
                case '\n':
                    break;
                default:
                    return false;
            }
            return true;
        }

        private void ConsumeSlash()
        {
            if (Match('/'))
            {
                //can this just be !match
                while (!IsAtEnd() && CurrentChar != '\n')
                    Advance();
            }
            else if (Match('*'))
            {
                while (!IsAtEnd())
                {
                    if (Match('*') && Match('/'))
                    {
                        break;
                    }
                    else
                    {
                        Advance();
                    }
                }
            }
            else
            {
                EmitTokenSingle(Match('=') ? TokenType.SLASH_EQUAL : TokenType.SLASH);
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDigit(int ch) => ch >= '0' && ch <= '9';

        public static bool IsAlpha(int c)
            => (c >= 'a' && c <= 'z') ||
                (c >= 'A' && c <= 'Z') ||
                c == '_';

        public static bool IsAlphaNumber(int c) => IsAlpha(c) || IsDigit(c);

        public void ConsumeDigit()
        {
            var _startingIndex = CurrentIndex;

            while (IsDigit(Peek()))
            {
                Advance();
            }

            if (Peek() == '.')
            {
                Advance();
                while (IsDigit(Peek()))
                {
                    Advance();
                }
            }

            var numStr = SubStrFrom(_startingIndex);

            EmitToken(TokenType.NUMBER, numStr);
        }

        public void ConsumeIdent()
        {
            var _startingIndex = CurrentIndex;

            while (IsAlphaNumber(Peek()))
            {
                Advance();
            }

            var token = IdentifierTokenType(_startingIndex, _index - _startingIndex + 1);
            EmitToken(token, token == TokenType.IDENTIFIER || token == TokenType.INIT ? SubStrFrom(_startingIndex) : null);
        }

        private TokenType IdentifierTokenType(int start, int length)
        {
            switch (length)
            {
                case 2:
                    if (Matches(start, "if")) return TokenType.IF;
                    if (Matches(start, "or")) return TokenType.OR;
                    break;
                case 3:
                    if (Matches(start, "and")) return TokenType.AND;
                    if (Matches(start, "for")) return TokenType.FOR;
                    if (Matches(start, "fun")) return TokenType.FUNCTION;
                    if (Matches(start, "var")) return TokenType.VAR;
                    break;
                case 4:
                    if (Matches(start, "else")) return TokenType.ELSE;
                    if (Matches(start, "enum")) return TokenType.ENUM;
                    if (Matches(start, "goto")) return TokenType.GOTO;
                    if (Matches(start, "init")) return TokenType.INIT;
                    if (Matches(start, "loop")) return TokenType.LOOP;
                    if (Matches(start, "null")) return TokenType.NULL;
                    if (Matches(start, "test")) return TokenType.TESTCASE;
                    if (Matches(start, "this")) return TokenType.THIS;
                    if (Matches(start, "true")) return TokenType.TRUE;
                    break;
                case 5:
                    if (Matches(start, "break")) return TokenType.BREAK;
                    if (Matches(start, "class")) return TokenType.CLASS;
                    if (Matches(start, "cname")) return TokenType.CONTEXT_NAME_CLASS;
                    if (Matches(start, "fname")) return TokenType.CONTEXT_NAME_FUNC;
                    if (Matches(start, "false")) return TokenType.FALSE;
                    if (Matches(start, "label")) return TokenType.LABEL;
                    if (Matches(start, "match")) return TokenType.MATCH;
                    if (Matches(start, "meets")) return TokenType.MEETS;
                    if (Matches(start, "mixin")) return TokenType.MIXIN;
                    if (Matches(start, "panic")) return TokenType.PANIC;
                    if (Matches(start, "signs")) return TokenType.SIGNS;
                    if (Matches(start, "tname")) return TokenType.CONTEXT_NAME_TEST;
                    if (Matches(start, "yield")) return TokenType.YIELD;
                    break;
                case 6:
                    if (Matches(start, "expect")) return TokenType.EXPECT;
                    if (Matches(start, "return")) return TokenType.RETURN;
                    if (Matches(start, "static")) return TokenType.STATIC;
                    if (Matches(start, "tsname")) return TokenType.CONTEXT_NAME_TESTSET;
                    if (Matches(start, "typeof")) return TokenType.TYPEOF;
                    break;
                case 7:
                    if (Matches(start, "countof")) return TokenType.COUNT_OF;
                    if (Matches(start, "testset")) return TokenType.TEST_SET;
                    break;
                case 8:
                    if (Matches(start, "continue")) return TokenType.CONTINUE;
                    if (Matches(start, "readonly")) return TokenType.READ_ONLY;
                    break;
            }

            return TokenType.IDENTIFIER;
        }

        private bool Matches(int start, string value)
        {
            for (var i = 0; i < value.Length; i++)
            {
                if (_source[start + i] != value[i])
                    return false;
            }

            return true;
        }

        public void ThrowScannerException(string msg)
        {
            var (line, characterNumber) = GetLineAndCharacterNumber();
            throw new ScannerException(msg, TokenType.IDENTIFIER, line, characterNumber, _script.Name);
        }

        public bool Match(char matchingCharToConsume)
        {
            if (Peek() == matchingCharToConsume)
            {
                Advance();
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Advance()
        {
            _index++;
            CurrentChar = (char)SafeRead(_index);
            if (CurrentChar == '\n' || CurrentChar == '\0')
            {
                _lineLengths.Add(_index - _previousRunningCount);
                _previousRunningCount = _index;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAtEnd() => Peek() == -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Peek() => SafeRead(_index + 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int SafeRead(int index)
        {
            return index < _source.Length ? _source[index] : -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EmitTokenSingle(TokenType token)
            => EmitToken(token, null);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EmitToken(TokenType simpleToken, string literal)
        {
            _tokens.Add(new Token(
                simpleToken,
                literal,
                _index));
        }

        public string SubStrFrom(int startingIndex)
        {
            var length = _index - startingIndex;
            return _script.Source.Substring(startingIndex, length + 1);
        }

        private (int line, int characterNumber) GetLineAndCharacterNumber()
        {
            return (_lineLengths.Count + 1, _index - _previousRunningCount + 1);
        }
    }
}
