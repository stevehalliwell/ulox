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

        private readonly List<IScannerTokenGenerator> _scannerGenerators = new();

        private Script _script;
        private List<Token> _tokens;

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

            this.AddGenerators(
                new StringScannerTokenGenerator(),  //needs the chance to steel } from direct symbol
                new DirectSymbolScannerMatchTokenGenerator(),
                new NumberScannerTokenGenerator(),
                identScannerGen
                );
        }

        public void AddGenerator(IScannerTokenGenerator gen)
            => _scannerGenerators.Add(gen);

        public void Reset()
        {
            _stringIterator = null;
            _script = default;
        }

        public List<Token> Scan(Script script)
        {
            SetScript(script);
            _tokens = new List<Token>(TokenStartingCapacity);

            while (!IsAtEnd())
            {
                Advance();
                var matchingGen = GetMatchingGenerator(CurrentChar);
                matchingGen.Consume(this);
            }

            EmitTokenSingle(TokenType.EOF);

            return _tokens;
        }

        public void SetScript(Script script)
        {
            _script = script;
            _stringIterator = new StringIterator(_script.Source);
        }

        public void ThrowScannerException(string msg)
        {
            throw new ScannerException(msg, TokenType.IDENTIFIER, _stringIterator.Line, _stringIterator.CharacterNumber, _script.Name);
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
        //this auto creating a string from char seems like a waste, what actually needs it other than dissembler and errors
        //  which could calc it when needed if it was null
        public void EmitTokenSingle(TokenType token)
            => EmitToken(token, null);//making this null, null doesn't even break anything

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EmitToken(TokenType simpleToken, object literal)
        {
            _tokens.Add(new Token(
                simpleToken,
                literal,
                _stringIterator.Line,
                _stringIterator.CharacterNumber,
                _stringIterator.CurrentIndex));
        }

        private void AddGenerators(params IScannerTokenGenerator[] scannerTokenGenerators)
        {
            foreach (var item in scannerTokenGenerators)
                AddGenerator(item);
        }

        private IScannerTokenGenerator GetMatchingGenerator(char ch)
        {
            var matchingGen = default(IScannerTokenGenerator);
            for (int i = 0; i < _scannerGenerators.Count; i++)
            {
                var gen = _scannerGenerators[i];
                if (gen.DoesMatchChar(ch))
                {
                    matchingGen = gen;
                    break;
                }
            }

            if (matchingGen == null)
                ThrowScannerException($"Unexpected character '{ch}'");

            return matchingGen;
        }
    }
}
