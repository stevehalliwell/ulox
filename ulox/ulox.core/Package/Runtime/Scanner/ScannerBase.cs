using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ULox
{
    public abstract class ScannerBase : IScanner
    {
        public List<Token> Tokens { get; private set; }
        public int Line { get; set; }
        public int CharacterNumber { get; set; }
        public Char CurrentChar { get; private set; }


        protected List<IScannerTokenGenerator> defaultGenerators = new List<IScannerTokenGenerator>();

        private StringReader _stringReader;
        public IdentifierScannerTokenGenerator IdentifierScannerTokenGenerator { get; } = new IdentifierScannerTokenGenerator();

        public ScannerBase()
        {
            AddGenerator(IdentifierScannerTokenGenerator);
            Reset();
        }

        public void AddGenerator(IScannerTokenGenerator gen)
            => defaultGenerators.Add(gen);

        public void Reset()
        {
            Tokens = new List<Token>();
            Line = 1;
            CharacterNumber = 0;
            if (_stringReader != null)
                _stringReader.Dispose();
        }

        public List<Token> Scan(string text)
        {
            using (_stringReader = new StringReader(text))
            {
                while (!IsAtEnd())
                {
                    Advance();
                    var ch = CurrentChar;

                    var matchinGen = defaultGenerators.FirstOrDefault(x => x.DoesMatchChar(ch));
                    if (matchinGen == null)
                        throw new ScannerException(TokenType.IDENTIFIER, Line, CharacterNumber, $"Unexpected character '{CurrentChar}'");

                    matchinGen.Consume(this);
                }

                AddTokenSingle(TokenType.EOF);
            }

            return Tokens;
        }

        public bool Match(Char matchingCharToConsume)
        {
            if (_stringReader.Peek() == matchingCharToConsume)
            {
                if (_stringReader.Read() == '\n')
                {
                    Line++;
                    CharacterNumber = 0;
                }
                CharacterNumber++;

                return true;
            }
            return false;
        }

        public void Advance()
        {
            CurrentChar = (Char)_stringReader.Read();
            CharacterNumber++;
        }

        public bool IsAtEnd()
            => _stringReader.Peek() == -1;

        public Char Peek()
            => (Char)_stringReader.Peek();

        public void ReadLine()
            => _stringReader.ReadLine();

        public void AddTokenSingle(TokenType token)
            => AddToken(token, CurrentChar.ToString(), null);

        public void AddToken(TokenType simpleToken, string str, object literal)
            => Tokens.Add(new Token(simpleToken, str, literal, Line, CharacterNumber));
    }
}
