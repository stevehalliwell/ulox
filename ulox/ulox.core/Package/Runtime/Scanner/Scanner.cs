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
        private StringIterator _stringIterator = new("");
        public char CurrentChar => _stringIterator.CurrentChar;
        public int CurrentIndex => _stringIterator.CurrentIndex;
        private Script _script;
        private List<Token> _tokens;
        private readonly StringBuilder _workingSpaceStringBuilder = new();
        private readonly Dictionary<string, TokenType> keywords = new()
        {
                {"var", TokenType.VAR},
                {"and", TokenType.AND},
                {"or", TokenType.OR},
                {"if", TokenType.IF},
                {"else", TokenType.ELSE},
                {"for", TokenType.FOR},
                {"loop", TokenType.LOOP},
                {"return", TokenType.RETURN},
                {"break", TokenType.BREAK},
                {"continue", TokenType.CONTINUE},
                {"true", TokenType.TRUE},
                {"false", TokenType.FALSE},
                {"null", TokenType.NULL},
                {"fun", TokenType.FUNCTION},
                {"panic", TokenType.PANIC},
                {"yield", TokenType.YIELD},
                {"fname", TokenType.CONTEXT_NAME_FUNC},

                {"testset", TokenType.TEST_SET},
                {"test", TokenType.TESTCASE},

                {"build", TokenType.BUILD},

                {"tname", TokenType.CONTEXT_NAME_TEST},
                {"tsname", TokenType.CONTEXT_NAME_TESTSET},

                {"class", TokenType.CLASS},
                {"mixin", TokenType.MIXIN},
                {ClassTypeCompilette.ThisName.String, TokenType.THIS},
                {"static", TokenType.STATIC},
                {"init", TokenType.INIT},
                {"cname", TokenType.CONTEXT_NAME_CLASS},

                {"typeof", TokenType.TYPEOF},

                {"meets", TokenType.MEETS},
                {"signs", TokenType.SIGNS},

                {"countof", TokenType.COUNT_OF},

                {"expect", TokenType.EXPECT},

                {"match", TokenType.MATCH},

                {"label", TokenType.LABEL},
                {"goto", TokenType.GOTO},

                {"enum", TokenType.ENUM},

                {"readonly", TokenType.READ_ONLY}
        };

        public Scanner()
        {
            Reset();
        }

        public void Reset()
        {
            _stringIterator = null;
            _script = default;
            _workingSpaceStringBuilder.Clear();
        }

        public TokenisedScript Scan(Script script)
        {
            _script = script;
            _stringIterator = new StringIterator(_script.Source);
            _tokens = new List<Token>(TokenStartingCapacity);

            while (!IsAtEnd())
            {
                Advance();
                Consume();
            }

            _stringIterator.FinishLineLengths();
            EmitTokenSingle(TokenType.EOF);

            return new TokenisedScript(_tokens, _stringIterator.LineLengths.ToArray(), script);
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

            var identString = SubStrFrom(_startingIndex);
            var token = TokenType.IDENTIFIER;

            if (keywords.TryGetValue(identString, out var keywordTokenType))
                token = keywordTokenType;

            EmitToken(token, identString);
        }

        public void ThrowScannerException(string msg)
        {
            var (line, characterNumber) = _stringIterator.GetLineAndCharacterNumber();
            throw new ScannerException(msg, TokenType.IDENTIFIER, line, characterNumber, _script.Name);
        }

        public bool Match(char matchingCharToConsume)
        {
            if (_stringIterator.Peek() == matchingCharToConsume)
            {
                Advance();
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Advance() => _stringIterator.Advance();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAtEnd() => _stringIterator.Peek() == -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public char Peek()
            => (char)_stringIterator.Peek();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EmitTokenSingle(TokenType token)
            => EmitToken(token, null);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EmitToken(TokenType simpleToken, string literal)
        {
            _tokens.Add(new Token(
                simpleToken,
                literal,
                _stringIterator.CurrentIndex));
        }

        public string SubStrFrom(int startingIndex)
        {
            var length = _stringIterator.CurrentIndex - startingIndex;
            return _script.Source.Substring(startingIndex, length + 1);
        }
    }
}
