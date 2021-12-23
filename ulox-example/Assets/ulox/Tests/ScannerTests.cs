using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace ULox.Tests
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
                TokenType.INT,
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
                TokenType.INT,
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
                TokenType.INT,
                TokenType.SLASH,
                TokenType.INT,
                TokenType.PERCENT,
                TokenType.INT,
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
            TokenType.INT,
            TokenType.LESS,
            TokenType.INT,
            TokenType.QUESTION,
            TokenType.INT,
            TokenType.COLON,
            TokenType.INT,
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
        
        [Test]
        [TestCaseSource(nameof(FloatDeclareTestStrings))]
        public void Scanner_FloatVarDeclare_TokenTypeMatch(string testString)
        {
            var tokenResults = new TokenType[]
            {
                TokenType.VAR,
                TokenType.IDENTIFIER,
                TokenType.ASSIGN,
                TokenType.FLOAT,
                TokenType.END_STATEMENT,
                TokenType.EOF
            };

            var scanner = new Scanner();

            scanner.Scan(testString);

            var resultingTokenTypes = scanner.Tokens.Select(x => x.TokenType).ToArray();

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
                TokenType.INT,
                TokenType.END_STATEMENT,
                TokenType.EOF
            };

            var scanner = new Scanner();

            scanner.Scan(testString);

            var resultingTokenTypes = scanner.Tokens.Select(x => x.TokenType).ToArray();

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

            var scanner = new Scanner();
            scanner.Scan(testString);

            var firstRes = scanner.Tokens;

            scanner.Reset();

            scanner.Scan(testString);

            Assert.AreEqual(firstRes, scanner.Tokens);
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

            var scanner = new Scanner();

            scanner.Scan(testString);

            var resultingTokenTypes = scanner.Tokens.Select(x => x.TokenType).ToArray();

            for (int i = 0; i < resultingTokenTypes.Length; i++)
            {
                Assert.AreEqual(tokenResults[i], resultingTokenTypes[i]);
            }
        }
        [Test]
        [TestCaseSource(nameof(Generator))]
        public void Scanner_TokenTypeMatch(string testString, TokenType[] tokenResults)
        {
            var scanner = new Scanner();

            scanner.Scan(testString);

            var resultingTokenTypes = scanner.Tokens.Select(x => x.TokenType).ToArray();

            var resString = string.Join(",", resultingTokenTypes.Select(x => x.ToString()).ToArray());

            for (int i = 0; i < resultingTokenTypes.Length; i++)
            {
                Assert.AreEqual(tokenResults[i], resultingTokenTypes[i]);
            }
        }
    }
}
