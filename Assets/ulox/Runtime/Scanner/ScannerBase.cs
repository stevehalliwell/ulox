using System;
using System.Collections.Generic;
using System.IO;

namespace ULox
{
    public abstract class ScannerBase
    {
        public List<Token> Tokens { get; private set; }
        public int Line { get; set; } 
        public int CharacterNumber { get; set; }
        public Char CurrentChar { get; private set; }

        protected List<IScannerTokenGenerator> defaultGenerators;
        protected Dictionary<char, IScannerCharMatchTokenGenerator> simpleGenerators;

        private StringReader _stringReader;

        public ScannerBase() => Reset();

        public void Reset()
        {
            Configure();

            Tokens = new List<Token>();
            Line = 1;
            CharacterNumber = 0;
            if (_stringReader != null)
                _stringReader.Dispose();
        }

        protected abstract void Configure();

        public List<Token> Scan(string text)
        {
            using (_stringReader = new StringReader(text))
            {
                while (!IsAtEnd())
                {
                    Advance();

                    if(simpleGenerators.TryGetValue(CurrentChar, out var foundSimpleGenerator))
                    {
                        foundSimpleGenerator.Consume(this);
                        continue;
                    }

                    var found = false;
                    foreach (var item in defaultGenerators)
                    {
                        if(item.DoesMatchChar(this))
                        {
                            item.Consume(this);
                            found = true;
                            break;
                        }
                    }

                    if(!found)
                        throw new ScannerException(TokenType.IDENTIFIER, Line, CharacterNumber, $"Unexpected character '{CurrentChar}'");
                           
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
