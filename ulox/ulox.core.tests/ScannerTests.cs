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
    }
}