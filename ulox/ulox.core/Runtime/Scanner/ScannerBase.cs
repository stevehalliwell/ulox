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

        public IdentifierScannerTokenGenerator IdentifierScannerTokenGenerator { get; private set; } = new IdentifierScannerTokenGenerator();

        protected List<IScannerTokenGenerator> defaultGenerators = new List<IScannerTokenGenerator>();
        protected Dictionary<char, IScannerCharMatchTokenGenerator> simpleGenerators = new Dictionary<char, IScannerCharMatchTokenGenerator>();

        private StringReader _stringReader;

        public ScannerBase()
        {
            AddDefaultGenerator(IdentifierScannerTokenGenerator);
            Reset();
        }

        public void AddCharMatchGenerator(IScannerCharMatchTokenGenerator gen)
            => simpleGenerators[gen.MatchingChar] = gen;

        public void AddDefaultGenerator(IScannerTokenGenerator gen)
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

                    if (simpleGenerators.TryGetValue(CurrentChar, out var foundSimpleGenerator))
                    {
                        foundSimpleGenerator.Consume(this);
                        continue;
                    }

                    var found = false;
                    foreach (var item in defaultGenerators)
                    {
                        if (item.DoesMatchChar(this))
                        {
                            item.Consume(this);
                            found = true;
                            break;
                        }
                    }

                    if (!found)
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
