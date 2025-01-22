using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ULox
{
    public interface IScannerTokenGenerator
    {
        bool DoesMatchChar(char ch);

        void Consume(Scanner scanner);
    }

    public sealed class Scanner
    {
        public const int TokenStartingCapacity = 500;
        private StringIterator _stringIterator = new("");
        public char CurrentChar => _stringIterator.CurrentChar;
        public int CurrentIndex => _stringIterator.CurrentIndex;

        private readonly List<IScannerTokenGenerator> _scannerGenerators = new();

        private Script _script;
        private List<Token> _tokens;
        private int _scannerGeneratorsCount;

        public Scanner()
        {
            Setup();
            Reset();
        }

        private void Setup()
        {
            var identScannerGen = new IdentifierScannerTokenGenerator();
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

                ("testset", TokenType.TEST_SET),
                ("test", TokenType.TESTCASE),

                ("build", TokenType.BUILD),

                ("tname", TokenType.CONTEXT_NAME_TEST),
                ("tsname", TokenType.CONTEXT_NAME_TESTSET),

                ("class", TokenType.CLASS),
                ("mixin", TokenType.MIXIN),
                (ClassTypeCompilette.ThisName.String, TokenType.THIS),
                ("static", TokenType.STATIC),
                ("init", TokenType.INIT),
                ("cname", TokenType.CONTEXT_NAME_CLASS),

                ("typeof", TokenType.TYPEOF),

                ("meets", TokenType.MEETS),
                ("signs", TokenType.SIGNS),

                ("countof", TokenType.COUNT_OF),

                ("expect", TokenType.EXPECT),

                ("match", TokenType.MATCH),

                ("label", TokenType.LABEL),
                ("goto", TokenType.GOTO),

                ("enum", TokenType.ENUM),

                ("readonly", TokenType.READ_ONLY),

                ("update", TokenType.UPDATE),

                ("soa", TokenType.SOA));


            _scannerGenerators.Add(new StringScannerTokenGenerator());
            _scannerGenerators.Add(new DirectSymbolScannerMatchTokenGenerator());
            _scannerGenerators.Add(new NumberScannerTokenGenerator());
            _scannerGenerators.Add(identScannerGen);

            _scannerGeneratorsCount = _scannerGenerators.Count;
        }

        public void Reset()
        {
            _stringIterator = null;
            _script = default;
        }

        public TokenisedScript Scan(Script script)
        {
            _script = script;
            _stringIterator = new StringIterator(_script.Source);
            _tokens = new List<Token>(TokenStartingCapacity);

            while (!IsAtEnd())
            {
                Advance();
                var matchingGen = GetMatchingGenerator(CurrentChar);
                matchingGen.Consume(this);
            }

            _stringIterator.FinishLineLengths();
            EmitTokenSingle(TokenType.EOF);

            return new TokenisedScript(_tokens, _stringIterator.LineLengths.ToArray(), script);
        }

        private IScannerTokenGenerator GetMatchingGenerator(char ch)
        {
            for (int i = 0; i < _scannerGeneratorsCount; i++)
            {
                var gen = _scannerGenerators[i];
                if (gen.DoesMatchChar(ch))
                {
                    return gen;
                }
            }

            ThrowScannerException($"Unexpected character '{ch}'");
            return null;
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
        public void ReadLine()
            => _stringIterator.ReadLine();

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
