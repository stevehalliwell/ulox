using NUnit.Framework;
using System.Collections.Generic;
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

        public static IEnumerable<TestCaseData> Generator()
        {
            yield return new TestCaseData(
@"fun foo(p)
{
    var a = p;
    var b = ""Hello"";
    fun bar()
    {
        var a = 7;
    }
    var res = bar();
}",
new TokenType[]
            {
                TokenType.FUNCTION,
                TokenType.IDENTIFIER,
                TokenType.OPEN_PAREN,
                TokenType.IDENTIFIER,
                TokenType.CLOSE_PAREN,
                TokenType.OPEN_BRACE,
                TokenType.VAR,
                TokenType.IDENTIFIER,
                TokenType.ASSIGN,
                TokenType.IDENTIFIER,
                TokenType.END_STATEMENT,
                TokenType.VAR,
                TokenType.IDENTIFIER,
                TokenType.ASSIGN,
                TokenType.STRING,
                TokenType.END_STATEMENT,
                TokenType.FUNCTION,
                TokenType.IDENTIFIER,
                TokenType.OPEN_PAREN,
                TokenType.CLOSE_PAREN,
                TokenType.OPEN_BRACE,
                TokenType.VAR,
                TokenType.IDENTIFIER,
                TokenType.ASSIGN,
                TokenType.NUMBER,
                TokenType.END_STATEMENT,
                TokenType.CLOSE_BRACE,
                TokenType.VAR,
                TokenType.IDENTIFIER,
                TokenType.ASSIGN,
                TokenType.IDENTIFIER,
                TokenType.OPEN_PAREN,
                TokenType.CLOSE_PAREN,
                TokenType.END_STATEMENT,
                TokenType.CLOSE_BRACE,
                TokenType.EOF,
            })
                .SetName("FunctionDeclareCall");

            yield return new TestCaseData(
@" var a = 1;
//var b = 2.1;
var c = ""hello"";
/*
    this is in a block comment so it's all gone
        including this /*
*/

var res = a * b + c - 1 / 2 % 9",
new TokenType[]
            {
                TokenType.VAR,
                TokenType.IDENTIFIER,
                TokenType.ASSIGN,
                TokenType.NUMBER,
                TokenType.END_STATEMENT,
                TokenType.VAR,
                TokenType.IDENTIFIER,
                TokenType.ASSIGN,
                TokenType.STRING,
                TokenType.END_STATEMENT,
                TokenType.VAR,
                TokenType.IDENTIFIER,
                TokenType.ASSIGN,
                TokenType.IDENTIFIER,
                TokenType.STAR,
                TokenType.IDENTIFIER,
                TokenType.PLUS,
                TokenType.IDENTIFIER,
                TokenType.MINUS,
                TokenType.NUMBER,
                TokenType.SLASH,
                TokenType.NUMBER,
                TokenType.PERCENT,
                TokenType.NUMBER,
                TokenType.EOF,
            })
                .SetName("Comments");

            yield return new TestCaseData(
@"var a = 1 < 2 ? 3 : 4;",
new TokenType[]
{
            TokenType.VAR,
            TokenType.IDENTIFIER,
            TokenType.ASSIGN,
            TokenType.NUMBER,
            TokenType.LESS,
            TokenType.NUMBER,
            TokenType.QUESTION,
            TokenType.NUMBER,
            TokenType.COLON,
            TokenType.NUMBER,
            TokenType.END_STATEMENT,
            TokenType.EOF,
})
                .SetName("Conditional");

            yield return new TestCaseData(
@"""hello"" + "" "" + ""world""",
new TokenType[]
{
            TokenType.STRING,
            TokenType.PLUS,
            TokenType.STRING,
            TokenType.PLUS,
            TokenType.STRING,
            TokenType.EOF,
})
                .SetName("SimpleString");
        }

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

            var tokens = scanner.Scan(new Script("test", testString));

            var resultingTokenTypes = tokens.Select(x => x.TokenType).ToArray();

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

            var tokens = scanner.Scan(new Script("test", testString));

            var resultingTokenTypes = tokens.Select(x => x.TokenType).ToArray();

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

            Assert.AreEqual(firstRes, res);
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

            var tokens = scanner.Scan(new Script("test", testString));

            var resultingTokenTypes = tokens.Select(x => x.TokenType).ToArray();

            for (int i = 0; i < resultingTokenTypes.Length; i++)
            {
                Assert.AreEqual(tokenResults[i], resultingTokenTypes[i]);
            }
        }

        [Test]
        [TestCaseSource(nameof(Generator))]
        public void Scanner_TokenTypeMatch(string testString, TokenType[] tokenResults)
        {
            var tokens = scanner.Scan(new Script("test", testString));

            var resultingTokenTypes = tokens.Select(x => x.TokenType).ToArray();

            var resString = string.Join(",", resultingTokenTypes.Select(x => x.ToString()).ToArray());

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

            var resultingTokenTypes = scanner.Scan(new Script("test", testString));

            CollectionAssert.AreEqual(tokenResults, resultingTokenTypes.Select(x => x.TokenType).ToArray());
        }
    }

    public class DesugarTests
    {
        [Test]
        public void Empty_WhenDesugar_DoesNotThrow()
        {
            var scriptContent = @"";
            var (_, tokenIterator) = Prepare(scriptContent);
            var res = new List<Token>();

            AdvanceGather(tokenIterator, res);

            Assert.AreEqual(1, res.Count);
        }

        [Test]
        public void DeclareVAr_WhenDesugar_ShouldHaveSame()
        {
            var scriptContent = @"var s = 1;";
            var (tokens, tokenIterator) = Prepare(scriptContent);
            var startingCount = tokens.Count;
            var res = new List<Token>();

            AdvanceGather(tokenIterator, res);

            Assert.AreEqual(res.Count, startingCount);
        }

        [Test]
        public void StringInterp_WhenDesugar_ShouldHaveMoreTokens()
        {
            var scriptContent = @"var s = ""Hi, {3}"";";
            var (tokens, tokenIterator) = Prepare(scriptContent);
            var startingCount = tokens.Count;
            var res = new List<Token>();

            AdvanceGather(tokenIterator, res);

            Assert.Greater(res.Count, startingCount);
        }

        private static (List<Token> tokens, TokenIterator tokenIterator) Prepare(string scriptContent)
        {
            var scanner = new Scanner();
            var script = new Script("test", scriptContent);
            var tokens = scanner.Scan(script);
            var tokenIterator = new TokenIterator(script, tokens);
            return (tokens, tokenIterator);
        }

        private static void AdvanceGather(TokenIterator tokenIterator, List<Token> list)
        {
            tokenIterator.Advance();

            while (tokenIterator.CurrentToken.TokenType != TokenType.EOF)
            {
                list.Add(tokenIterator.CurrentToken);
                tokenIterator.Advance();
            }

            list.Add(tokenIterator.CurrentToken);
        }
    }
}