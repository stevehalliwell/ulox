using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ULox
{
    public class Scanner
    {
        public List<Token> Tokens { get; private set; }
        private int _line, _characterNumber;
        private StringReader _stringReader;
        private StringBuilder workingSpaceStringBuilder;
        private Char _currentChar;

        private Dictionary<string, TokenType> keywords = new Dictionary<string, TokenType>()
        {
            { "var",    TokenType.VAR},
            { "string", TokenType.STRING},
            { "int",    TokenType.INT},
            { "float",  TokenType.FLOAT},
            { "and",    TokenType.AND},
            { "or",     TokenType.OR},
            { "if",     TokenType.IF},
            { "else",   TokenType.ELSE},
            { "while",  TokenType.WHILE},
            { "for",    TokenType.FOR},
            { "loop",   TokenType.LOOP},
            { "return", TokenType.RETURN},
            { "break",  TokenType.BREAK},
            { "continue", TokenType.CONTINUE},
            { "true",   TokenType.TRUE},
            { "false",  TokenType.FALSE},
            { "null",   TokenType.NULL},
            { "fun",    TokenType.FUNCTION},
            { "class",  TokenType.CLASS},
            { "this",  TokenType.THIS},
            { "super",  TokenType.SUPER},
            { ".",      TokenType.DOT},
            { "throw",  TokenType.THROW},
            { "test",  TokenType.TEST},
            { "testcase",  TokenType.TESTCASE},
            { "static",  TokenType.STATIC},
        };

        public Scanner()
        {
            Reset();
        }

        public void Reset()
        {
            Tokens = new List<Token>();
            _line = 1;
            _characterNumber = 0;
            if (_stringReader != null)
                _stringReader.Dispose();
            workingSpaceStringBuilder = new StringBuilder();
        }

        public List<Token> Scan(string text)
        {
            using (_stringReader = new StringReader(text))
            {
                while (!IsAtEnd())
                {
                    Advance();

                    switch (_currentChar)
                    {
                        case '(': AddTokenSingle(TokenType.OPEN_PAREN); break;
                        case ')': AddTokenSingle(TokenType.CLOSE_PAREN); break;
                        case '{': AddTokenSingle(TokenType.OPEN_BRACE); break;
                        case '}': AddTokenSingle(TokenType.CLOSE_BRACE); break;
                        case ',': AddTokenSingle(TokenType.COMMA); break;
                        case '.': AddTokenSingle(TokenType.DOT); break;
                        case ';': AddTokenSingle(TokenType.END_STATEMENT); break;
                        case '-':
                            AddTokenSingle(Match('=') ? TokenType.MINUS_EQUAL :
                                (Match('-') ? TokenType.DECREMENT : TokenType.MINUS)); break;
                        case '+':
                            AddTokenSingle(Match('=') ? TokenType.PLUS_EQUAL :
                                (Match('+') ? TokenType.INCREMENT : TokenType.PLUS)); break;
                        case '*': AddTokenSingle(Match('=') ? TokenType.STAR_EQUAL : TokenType.STAR); break;
                        case '%': AddTokenSingle(Match('=') ? TokenType.PERCENT_EQUAL : TokenType.PERCENT); break;
                        case ':': AddTokenSingle(TokenType.COLON); break;
                        case '?': AddTokenSingle(TokenType.QUESTION); break;

                        case '!': AddTokenSingle(Match('=') ? TokenType.BANG_EQUAL : TokenType.BANG); break;
                        case '=': AddTokenSingle(Match('=') ? TokenType.EQUALITY : TokenType.ASSIGN); break;
                        case '<': AddTokenSingle(Match('=') ? TokenType.LESS_EQUAL : TokenType.LESS); break;
                        case '>': AddTokenSingle(Match('=') ? TokenType.GREATER_EQUAL : TokenType.GREATER); break;

                        case '/':
                            {
                                if (Match('/'))
                                    {
                                        _stringReader.ReadLine();
                                        _line++;
                                    }
                                    else if (Match('*'))
                                    {
                                        ConsumeBlockComment();
                                    }
                                    else
                                    {
                                        AddTokenSingle(Match('=') ? TokenType.SLASH_EQUAL : TokenType.SLASH);
                                    }
                                break;
                            }

                        case ' ':
                        case '\r':
                        case '\t':
                            //skiping over whitespace
                            _characterNumber++;
                            break;

                        case '\n':
                            _line++;
                            _characterNumber = 0;
                            break;

                        case '"': ConsumeString(); break;

                        default:
                        {
                            if (IsDigit(_currentChar))
                            {
                                ConsumeNumber();
                            }
                            else if (IsAlpha(_currentChar))
                            {
                                ConsumeIdentifier();
                            }
                            else
                            {
                                throw new ScannerException(TokenType.IDENTIFIER, _line, _characterNumber, $"Unexpected character '{(char)_currentChar}'");
                            }
                            break;
                        }
                    }
                }

                AddTokenSingle(TokenType.EOF);
            }

            return Tokens;
        }

        private void ConsumeBlockComment()
        {
            while (!IsAtEnd())
            {
                if (Match('*') && Match('/'))
                {
                    return;
                }
                else
                {
                    Advance();
                }
            }
        }

        private void ConsumeIdentifier()
        {
            workingSpaceStringBuilder.Clear();

            workingSpaceStringBuilder.Append(_currentChar);

            while (IsAlphaNumber(Peek()))
            {
                Advance();
                workingSpaceStringBuilder.Append(_currentChar);
            }

            var identString = workingSpaceStringBuilder.ToString();
            var token = TokenType.IDENTIFIER;

            if (keywords.TryGetValue(identString, out var keywordTokenType))
                token = keywordTokenType;

            AddToken(token, identString, identString);
        }

        private void ConsumeNumber()
        {
            bool hasFoundDecimalPoint = false;
            workingSpaceStringBuilder.Clear();

            workingSpaceStringBuilder.Append(_currentChar);

            while (IsDigit(Peek()))
            {
                Advance();
                workingSpaceStringBuilder.Append(_currentChar);
            }

            if (Peek() == '.')
            {
                Advance();
                workingSpaceStringBuilder.Append(_currentChar);
                hasFoundDecimalPoint = true;
                while (IsDigit(Peek()))
                {
                    Advance();
                    workingSpaceStringBuilder.Append(_currentChar);
                }
            }

            var numStr = workingSpaceStringBuilder.ToString();

            AddToken(hasFoundDecimalPoint ? TokenType.FLOAT : TokenType.INT,
                numStr,
                double.Parse(numStr));
        }

        private void ConsumeString()
        {
            var startingLine = _line;
            var startingChar = _characterNumber;
            workingSpaceStringBuilder.Clear();
            Advance();//skip leading "
            while (!IsAtEnd())
            {
                if (_currentChar == '\n') { _line++; _characterNumber = 0; }

                if (_currentChar == '"')
                {
                    var str = System.Text.RegularExpressions.Regex.Unescape(workingSpaceStringBuilder.ToString());
                    AddToken(TokenType.STRING, str, str);
                    return;
                }

                workingSpaceStringBuilder.Append(_currentChar);

                Advance();
            }

            //we don't want this but when doing expression only mode the last char and the close of quote can be the same
            if (_currentChar == '"')
            {
                var str = System.Text.RegularExpressions.Regex.Unescape(workingSpaceStringBuilder.ToString());
                AddToken(TokenType.STRING, str, str);
                return;
            }

            throw new ScannerException(TokenType.IDENTIFIER, _line, _characterNumber, "Unterminated String");
        }

        private void Advance()
        {
            _currentChar = (Char)_stringReader.Read();
            _characterNumber++;
        }

        private static bool IsDigit(int ch)
        {
            return ch >= '0' && ch <= '9';
        }

        private static bool IsAlpha(int c)
        {
            return (c >= 'a' && c <= 'z') ||
                (c >= 'A' && c <= 'Z') ||
                c == '_';
        }

        private static bool IsAlphaNumber(int c)
        {
            return IsAlpha(c) || IsDigit(c);
        }

        private bool IsAtEnd()
        {
            return _stringReader.Peek() == -1;
        }

        private Char Peek()
        {
            return (Char)_stringReader.Peek();
        }

        private bool Match(Char matchingCharToConsume)
        {
            if (_stringReader.Peek() == matchingCharToConsume)
            {
                if (_stringReader.Read() == '\n')
                {
                    _line++;
                    _characterNumber = 0;
                }
                _characterNumber++;

                return true;
            }
            return false;
        }

        private void AddTokenSingle(TokenType token)
        {
            AddToken(token, _currentChar.ToString(), null);
        }

        private void AddToken(TokenType simpleToken, string str, object literal)
        {
            Tokens.Add(new Token(simpleToken, str, literal, _line, _characterNumber));
        }
    }
}
