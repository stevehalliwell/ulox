using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ULox
{
    public class Scanner : IScanner
    {
        public List<Token> Tokens { get; private set; }
        public int Line { get; set; }
        public int CharacterNumber { get; set; }
        public Char CurrentChar { get; private set; }
         
        protected List<IScannerTokenGenerator> defaultGenerators = new List<IScannerTokenGenerator>();

        private StringReader _stringReader;

        public Scanner()
        {
            Setup();
            Reset();
        }

        private void Setup()
        {
            var identScannerGen = new IdentifierScannerTokenGenerator();

            this.AddGenerators(
                new WhiteSpaceScannerTokenGenerator(),
                new StringScannerTokenGenerator(),
                new NumberScannerTokenGenerator(),
                new SlashScannerTokenGenerator(),
                new CompoundCharScannerCharMatchTokenGenerator(),
                identScannerGen
                                    );

            identScannerGen.Add(
                ("var", TokenType.VAR),
                ("string", TokenType.STRING),
                ("int", TokenType.INT),
                ("float", TokenType.FLOAT),
                ("and", TokenType.AND),
                ("or", TokenType.OR),
                ("if", TokenType.IF),
                ("else", TokenType.ELSE),
                ("while", TokenType.WHILE),
                ("for", TokenType.FOR),
                ("loop", TokenType.LOOP),
                ("return", TokenType.RETURN),
                ("break", TokenType.BREAK),
                ("continue", TokenType.CONTINUE),
                ("true", TokenType.TRUE),
                ("false", TokenType.FALSE),
                ("null", TokenType.NULL),
                ("fun", TokenType.FUNCTION),
                ("throw", TokenType.THROW),
                ("yield", TokenType.YIELD),
                ("fname", TokenType.CONTEXT_NAME_FUNC),

                ("test", TokenType.TEST),
                ("testcase", TokenType.TESTCASE),

                ("build", TokenType.BUILD),

                ("tcname", TokenType.CONTEXT_NAME_TESTCASE),
                ("tsname", TokenType.CONTEXT_NAME_TESTSET),

                ("class", TokenType.CLASS),
                ("mixin", TokenType.MIXIN),
                ("this", TokenType.THIS),
                ("super", TokenType.SUPER),
                ("static", TokenType.STATIC),
                ("init", TokenType.INIT),
                ("cname", TokenType.CONTEXT_NAME_CLASS),
                ("freeze", TokenType.FREEZE),

                ("inject", TokenType.INJECT),
                ("register", TokenType.REGISTER),

                ("typeof", TokenType.TYPEOF),

                ("local", TokenType.LOCAL),
                ("pure", TokenType.PURE),

                ("meets", TokenType.MEETS)
                                              );

            this.AddSingleCharTokenGenerators(
                ('(', TokenType.OPEN_PAREN),
                (')', TokenType.CLOSE_PAREN),
                ('{', TokenType.OPEN_BRACE),
                ('}', TokenType.CLOSE_BRACE),
                ('[', TokenType.OPEN_BRACKET),
                (']', TokenType.CLOSE_BRACKET),
                (',', TokenType.COMMA),
                (';', TokenType.END_STATEMENT),
                ('.', TokenType.DOT),
                (':', TokenType.COLON),
                ('?', TokenType.QUESTION)
                                                    );
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


        private void AddSingleCharTokenGenerator(char ch, TokenType tt)
            => AddGenerator(new ConfiguredSingleCharScannerCharMatchTokenGenerator(ch, tt));

        private void AddSingleCharTokenGenerators(params (char ch, TokenType token)[] tokens)
        {
            foreach (var item in tokens)
                AddSingleCharTokenGenerator(item.ch, item.token);
        }

        private void AddGenerators(params IScannerTokenGenerator[] scannerTokenGenerators)
        {
            foreach (var item in scannerTokenGenerators)
                AddGenerator(item);
        }
    }
}
