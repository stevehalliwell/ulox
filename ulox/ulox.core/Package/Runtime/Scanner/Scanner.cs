using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class Scanner
    {
        public const int TokenStartingCapacity = 500;
        private StringIterator _stringIterator = new StringIterator("");
        public char CurrentChar => _stringIterator.CurrentChar;

        public static readonly Token NoTokenFound = new Token(TokenType.NONE, string.Empty, null, 0, 0, -1);
        public static Token SharedNoToken => NoTokenFound;

        private readonly List<IScannerTokenGenerator> _scannerGenerators = new List<IScannerTokenGenerator>();

        private Script _script;

        public Scanner()
        {
            Setup();
            Reset();
        }

        private void Setup()
        {
            var identScannerGen = new IdentifierScannerTokenGenerator();

            // ensure we add these in the order as we expect them to occur by most to least frequent
            this.AddGenerators(
                new DirectSymbolScannerMatchTokenGenerator(),
                new NumberScannerTokenGenerator(),
                new StringScannerTokenGenerator(),
                identScannerGen
                );

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
                ("this", TokenType.THIS),
                ("static", TokenType.STATIC),
                ("init", TokenType.INIT),
                ("cname", TokenType.CONTEXT_NAME_CLASS),
                ("freeze", TokenType.FREEZE),

                ("typeof", TokenType.TYPEOF),

                ("meets", TokenType.MEETS),
                ("signs", TokenType.SIGNS),

                ("countof", TokenType.COUNT_OF),

                ("expect", TokenType.EXPECT),

                ("data", TokenType.DATA),
                ("system", TokenType.SYSTEM),

                ("match", TokenType.MATCH),

                ("label", TokenType.LABEL),
                ("goto", TokenType.GOTO),

                ("enum", TokenType.ENUM),

                ("readonly", TokenType.READ_ONLY),

                ("update", TokenType.UPDATE));
        }

        public void AddGenerator(IScannerTokenGenerator gen)
            => _scannerGenerators.Add(gen);

        public string GetSourceSection(int start, int len)
        {
            return _script.Source.Substring(start, len);
        }

        public void Reset()
        {
            _stringIterator = null;
            _script = default;
        }
        
        public Token Next()
        {
            while (!IsAtEnd())
            {
                Advance();
                var matchinGen = GetMatchingGenerator(CurrentChar);
                var tok = matchinGen.Consume(this);
                if (tok.TokenType != TokenType.NONE)
                    return tok;
            }

            return EmitTokenSingle(TokenType.EOF);
        }

        public List<Token> Scan(Script script)
        {
            SetScript(script);
            var tokens = new List<Token>(TokenStartingCapacity);
            var lastToken = default(Token);
            do
            {
                lastToken = Next();
                tokens.Add(lastToken);
            } while (lastToken.TokenType != TokenType.EOF);
            
            return tokens;
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
        public Token EmitTokenSingle(TokenType token)
            => EmitToken(token, CurrentChar.ToString(), null);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Token EmitToken(TokenType simpleToken, string str, object literal)
            => new Token(simpleToken, str, literal, _stringIterator.Line, _stringIterator.CharacterNumber, _stringIterator.CurrentIndex);

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
