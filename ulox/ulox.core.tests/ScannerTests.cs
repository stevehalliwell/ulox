using NUnit.Framework;
using System.Linq;

namespace ULox.Core.Tests
{
    public class ScannerTests
    {

        private static string[] FloatDeclareTestStrings = new string[]
        {
            @"var PI = 3.14;",
            @"var PI= 3.14;",
            @"var PI =3.14;",
            @"var PI=3.14;",
            @"var
PI
=
3.14
;",
            @"var PI = 3.14;",
        };

        private static string[] IntDeclareTestStrings = new string[]
        {
            @"var rand = 7;",
            @"var rand=7;",
            @"var rand =7;",
            @"var rand= 7;",
            @"var rand= 7 ;",
            @"var   rand    =   7   ;",
            @"var
rand
=
7
;",
            @"var rand = 71234 ;",
        };

        private static string[] StringDeclareTestStrings = new string[]
                        {
            @"var lang = ""lox"";",
            @"var lang=""lox"";",
            @"var lang
=
""lox""
;",
            @"var multi = ""Now is the winter of our discontent
Made glorious summer by this sun of York;
And all the clouds that lour'd upon our house
In the deep bosom of the ocean buried.""; ",
        };

        private Scanner scanner;

        [SetUp]
        public void SetUp()
        {
            scanner = new Scanner();
        }

        [Test]
        [TestCaseSource(nameof(FloatDeclareTestStrings))]
        public void Scanner_FloatVarDeclare_TokenTypeMatch(string testString)
        {
            var tokenResults = new TokenType[]
            {
                TokenType.VAR,
                TokenType.IDENTIFIER,
                TokenType.ASSIGN,
                TokenType.NUMBER,
                TokenType.END_STATEMENT,
                TokenType.EOF
            };

            var tokenisedScript = scanner.Scan(new Script("test", testString));

            var resultingTokenTypes = tokenisedScript.Tokens.Select(x => x.TokenType).ToArray();

            for (int i = 0; i < resultingTokenTypes.Length; i++)
            {
                Assert.AreEqual(tokenResults[i], resultingTokenTypes[i]);
            }
        }

        [Test]
        [TestCaseSource(nameof(IntDeclareTestStrings))]
        public void Scanner_IntVarDeclare_TokenTypeMatch(string testString)
        {
            var tokenResults = new TokenType[]
            {
                TokenType.VAR,
                TokenType.IDENTIFIER,
                TokenType.ASSIGN,
                TokenType.NUMBER,
                TokenType.END_STATEMENT,
                TokenType.EOF
            };

            var tokenisedScript = scanner.Scan(new Script("test", testString));

            var resultingTokenTypes = tokenisedScript.Tokens.Select(x => x.TokenType).ToArray();

            for (int i = 0; i < resultingTokenTypes.Length; i++)
            {
                Assert.AreEqual(tokenResults[i], resultingTokenTypes[i]);
            }
        }

        [Test]
        public void Scanner_Reset_SameResult()
        {
            var testString = @"var a = 1; a = 2 * a;
fun foo(p)
{
    var a = p;
    var b = ""Hello"";
    fun bar()
    {
        var a = 7;
    }
    var res = bar();
}";

            var firstRes = scanner.Scan(new Script("test", testString));

            scanner.Reset();

            var res = scanner.Scan(new Script("test", testString));

            CollectionAssert.AreEqual(firstRes.Tokens, res.Tokens);
        }

        [Test]
        [TestCaseSource(nameof(StringDeclareTestStrings))]
        public void Scanner_StringVarDeclare_TokenTypeMatch(string testString)
        {
            var tokenResults = new TokenType[]
            {
                TokenType.VAR,
                TokenType.IDENTIFIER,
                TokenType.ASSIGN,
                TokenType.STRING,
                TokenType.END_STATEMENT,
                TokenType.EOF
            };

            var tokenisedScript = scanner.Scan(new Script("test", testString));

            var resultingTokenTypes = tokenisedScript.Tokens.Select(x => x.TokenType).ToArray();

            for (int i = 0; i < resultingTokenTypes.Length; i++)
            {
                Assert.AreEqual(tokenResults[i], resultingTokenTypes[i]);
            }
        }

        [Test]
        public void Scanner_WhenUnterminatedString_ShouldThrow()
        {
            var testString = @"var a = ""hello";

            void Act() => scanner.Scan(new Script("test", testString));

            var ex = Assert.Throws<ScannerException>(Act);
            Assert.AreEqual("Unterminated String got IDENTIFIER in test at 1:14", ex.Message);
        }

        [Test]
        public void Scanner_DirectSymbolsAndCompoundOperators_EmitsExactTokenTypes()
        {
            var testString = "(){}[],;.: + += - -= * *= % %= ! != = == < <= > >= / /= """"";
            var expected = new[]
            {
                TokenType.OPEN_PAREN,
                TokenType.CLOSE_PAREN,
                TokenType.OPEN_BRACE,
                TokenType.CLOSE_BRACE,
                TokenType.OPEN_BRACKET,
                TokenType.CLOSE_BRACKET,
                TokenType.COMMA,
                TokenType.END_STATEMENT,
                TokenType.DOT,
                TokenType.COLON,
                TokenType.PLUS,
                TokenType.PLUS_EQUAL,
                TokenType.MINUS,
                TokenType.MINUS_EQUAL,
                TokenType.STAR,
                TokenType.STAR_EQUAL,
                TokenType.PERCENT,
                TokenType.PERCENT_EQUAL,
                TokenType.BANG,
                TokenType.BANG_EQUAL,
                TokenType.ASSIGN,
                TokenType.EQUALITY,
                TokenType.LESS,
                TokenType.LESS_EQUAL,
                TokenType.GREATER,
                TokenType.GREATER_EQUAL,
                TokenType.SLASH,
                TokenType.SLASH_EQUAL,
                TokenType.STRING,
                TokenType.EOF,
            };

            AssertTokenTypes(testString, expected);
        }

        [Test]
        public void Scanner_AllKeywords_EmitKeywordTokensWithOriginalLiteral()
        {
            var testString = "var and or if else for loop return break continue true false null fun panic yield fname testset test tname tsname class mixin this static init cname typeof meets signs countof expect match label goto enum readonly";
            var expected = new[]
            {
                TokenType.VAR,
                TokenType.AND,
                TokenType.OR,
                TokenType.IF,
                TokenType.ELSE,
                TokenType.FOR,
                TokenType.LOOP,
                TokenType.RETURN,
                TokenType.BREAK,
                TokenType.CONTINUE,
                TokenType.TRUE,
                TokenType.FALSE,
                TokenType.NULL,
                TokenType.FUNCTION,
                TokenType.PANIC,
                TokenType.YIELD,
                TokenType.CONTEXT_NAME_FUNC,
                TokenType.TEST_SET,
                TokenType.TESTCASE,
                TokenType.CONTEXT_NAME_TEST,
                TokenType.CONTEXT_NAME_TESTSET,
                TokenType.CLASS,
                TokenType.MIXIN,
                TokenType.THIS,
                TokenType.STATIC,
                TokenType.INIT,
                TokenType.CONTEXT_NAME_CLASS,
                TokenType.TYPEOF,
                TokenType.MEETS,
                TokenType.SIGNS,
                TokenType.COUNT_OF,
                TokenType.EXPECT,
                TokenType.MATCH,
                TokenType.LABEL,
                TokenType.GOTO,
                TokenType.ENUM,
                TokenType.READ_ONLY,
                TokenType.EOF,
            };

            var tokenisedScript = scanner.Scan(new Script("test", testString));

            CollectionAssert.AreEqual(expected, tokenisedScript.Tokens.Select(x => x.TokenType).ToArray());
            CollectionAssert.AreEqual(testString.Split(' '), tokenisedScript.Tokens.Take(expected.Length - 1).Select(x => x.Literal).ToArray());
        }

        [Test]
        public void Scanner_IdentifiersThatContainKeywords_StayIdentifiers()
        {
            var testString = "variant andy orchard iffy elsewhere foreach looped returnValue breaker continue_ trueValue falsehood nullable functionName panicked yielding fname2 testset_ testcase tname_ tsname_ className mixins this_ staticValue initial cname_ typeof_ meets_ signs_ countof_ expect_ match_ label_ goto_ enum_ readonly_";

            var tokenisedScript = scanner.Scan(new Script("test", testString));

            Assert.That(tokenisedScript.Tokens.Take(tokenisedScript.Tokens.Count - 1).All(x => x.TokenType == TokenType.IDENTIFIER), Is.True);
            CollectionAssert.AreEqual(testString.Split(' '), tokenisedScript.Tokens.Take(tokenisedScript.Tokens.Count - 1).Select(x => x.Literal).ToArray());
        }

        [Test]
        public void Scanner_NumberLiterals_KeepExactSourceText()
        {
            var testString = "var a = 0; var b = 00123; var c = 3.1400; var d = 7.;";

            var tokenisedScript = scanner.Scan(new Script("test", testString));

            CollectionAssert.AreEqual(
                new[] { "0", "00123", "3.1400", "7." },
                tokenisedScript.Tokens.Where(x => x.TokenType == TokenType.NUMBER).Select(x => x.Literal).ToArray());
        }

        [Test]
        public void Scanner_StringWithEscapedQuote_KeepsEscapeInLiteral()
        {
            var testString = @"var s = ""a \""quoted\"" word"";";

            var tokenisedScript = scanner.Scan(new Script("test", testString));

            var str = tokenisedScript.Tokens.Single(x => x.TokenType == TokenType.STRING);
            Assert.AreEqual(@"a \""quoted\"" word", str.Literal);
        }

        [Test]
        public void Scanner_MultilineString_KeepsNewlineInLiteralAndContinuesAfterCloseQuote()
        {
            var testString = "var s = \"line one\nline two\"; var n = 1;";

            var tokenisedScript = scanner.Scan(new Script("test", testString));

            var str = tokenisedScript.Tokens.Single(x => x.TokenType == TokenType.STRING);
            Assert.AreEqual("line one\nline two", str.Literal);
            CollectionAssert.AreEqual(
                new[] { TokenType.VAR, TokenType.IDENTIFIER, TokenType.ASSIGN, TokenType.STRING, TokenType.END_STATEMENT, TokenType.VAR, TokenType.IDENTIFIER, TokenType.ASSIGN, TokenType.NUMBER, TokenType.END_STATEMENT, TokenType.EOF },
                tokenisedScript.Tokens.Select(x => x.TokenType).ToArray());
        }

        [Test]
        public void Scanner_LineComment_SkipsOnlyUntilNewline()
        {
            var testString = "var a = 1; // var ignored = 2;\nvar b = 3;";

            var tokenisedScript = scanner.Scan(new Script("test", testString));

            CollectionAssert.AreEqual(
                new[] { "var", "a", "1", "var", "b", "3" },
                tokenisedScript.Tokens.Where(x => x.Literal != null).Select(x => x.Literal).ToArray());
        }

        [Test]
        public void Scanner_BlockComment_SkipsUntilCloseThenContinues()
        {
            var testString = "var a = 1; /* var ignored = 2;\n still ignored */ var b = 3;";

            var tokenisedScript = scanner.Scan(new Script("test", testString));

            CollectionAssert.AreEqual(
                new[] { "var", "a", "1", "var", "b", "3" },
                tokenisedScript.Tokens.Where(x => x.Literal != null).Select(x => x.Literal).ToArray());
        }

        [Test]
        public void Scanner_UnexpectedCharacterOnSecondLine_ThrowsWithLineAndCharacter()
        {
            var testString = "var a = 1;\n$";

            var ex = Assert.Throws<ScannerException>(() => scanner.Scan(new Script("fail", testString)));

            Assert.AreEqual("Unexpected character '$' got IDENTIFIER in fail at 2:1", ex.Message);
        }

        [Test]
        public void Scanner_UnterminatedStringAfterNewline_ThrowsAtEndLocation()
        {
            var testString = "var a = \"hello\nworld";

            var ex = Assert.Throws<ScannerException>(() => scanner.Scan(new Script("fail", testString)));

            Assert.AreEqual("Unterminated String got IDENTIFIER in fail at 2:6", ex.Message);
        }

        [Test]
        public void StringInterp_WhenEscaped_ShouldMatchTokens()
        {
            var tokenResults = new TokenType[]
            {
                TokenType.VAR,
                TokenType.IDENTIFIER,
                TokenType.ASSIGN,
                TokenType.STRING,
                TokenType.END_STATEMENT,
                TokenType.EOF
            };
            var testString = @"var s = ""Hi, \{3}"";";

            var tokenisedScript = scanner.Scan(new Script("test", testString));

            var resultingTokenTypes = tokenisedScript.Tokens.Select(x => x.TokenType).ToArray();

            CollectionAssert.AreEqual(tokenResults, resultingTokenTypes);
        }

        private void AssertTokenTypes(string testString, TokenType[] expected)
        {
            var tokenisedScript = scanner.Scan(new Script("test", testString));
            CollectionAssert.AreEqual(expected, tokenisedScript.Tokens.Select(x => x.TokenType).ToArray());
        }
    }
}