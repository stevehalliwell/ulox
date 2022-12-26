using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class Scanner : IScanner
    {
        private StringIterator _stringIterator = new StringIterator("");
        public List<Token> Tokens { get; private set; }
        public char CurrentChar => _stringIterator.CurrentChar;

        private readonly List<IScannerTokenGenerator> defaultGenerators = new List<IScannerTokenGenerator>();

        private Script _script;

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
                new SingleCharScannerCharMatchTokenGenerator(),
                identScannerGen);

            identScannerGen.Add(
                ("var", TokenType.VAR),
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
                ("static", TokenType.STATIC),
                ("init", TokenType.INIT),
                ("cname", TokenType.CONTEXT_NAME_CLASS),
                ("freeze", TokenType.FREEZE),

                ("inject", TokenType.INJECT),
                ("register", TokenType.REGISTER),

                ("typeof", TokenType.TYPEOF),

                ("local", TokenType.LOCAL),
                ("pure", TokenType.PURE),

                ("meets", TokenType.MEETS),
                ("signs", TokenType.SIGNS),

                ("countof", TokenType.COUNT_OF),

                ("expect", TokenType.EXPECT),

                ("data", TokenType.DATA),

                ("match", TokenType.MATCH),

                ("label", TokenType.LABEL),
                ("goto", TokenType.GOTO),

                ("enum", TokenType.ENUM),

                ("readonly", TokenType.READ_ONLY));
        }

        public void AddGenerator(IScannerTokenGenerator gen)
            => defaultGenerators.Add(gen);

        public void Reset()
        {
            Tokens = new List<Token>();
            _stringIterator = null;
            _script = default;
        }

        public List<Token> Scan(Script script)
        {
            _script = script;

            _stringIterator = new StringIterator(_script.Source);
            while (!IsAtEnd())
            {
                Advance();
                var ch = CurrentChar;
                DoGenerator(ch);
            }

            AddTokenSingle(TokenType.EOF);
            return Tokens;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ThrowScannerException(string msg)
        {
            throw new ScannerException(msg, TokenType.IDENTIFIER, _stringIterator.Line, _stringIterator.CharacterNumber, _script.Name);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Match(Char matchingCharToConsume)
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
        public bool IsAtEnd()
            => _stringIterator.Peek() == -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Char Peek()
            => (Char)_stringIterator.Peek();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadLine()
            => _stringIterator.ReadLine();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddTokenSingle(TokenType token)
            => AddToken(token, CurrentChar.ToString(), null);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddToken(TokenType simpleToken, string str, object literal)
            => Tokens.Add(new Token(simpleToken, str, literal, _stringIterator.Line, _stringIterator.CharacterNumber));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddGenerators(params IScannerTokenGenerator[] scannerTokenGenerators)
        {
            foreach (var item in scannerTokenGenerators)
                AddGenerator(item);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoGenerator(char ch)
        {
            var matchinGen = default(IScannerTokenGenerator);
            foreach (var gen in defaultGenerators)
            {
                if (gen.DoesMatchChar(ch))
                {
                    matchinGen = gen;
                    break;
                }
            }

            if (matchinGen != null)
                matchinGen.Consume(this);
            else
                ThrowScannerException($"Unexpected character '{ch}'");
        }
    }
}
