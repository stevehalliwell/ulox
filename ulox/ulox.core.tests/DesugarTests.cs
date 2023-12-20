using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ULox.Core.Tests
{
    public class DesugarTests
    {
        public TestContext TestContext { get; set; }

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
        public void DeclareVar_WhenDesugar_ShouldHaveSame()
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

        [Test]
        public void While_WhenDesugar_ShouldBeFor()
        {
            var scriptContent = @"
var i = 1;
while(i<10)
{
i+= 1;
}";
            var (tokens, tokenIterator) = Prepare(scriptContent);
            var startingCount = tokens.Count;
            var res = new List<Token>();

            AdvanceGather(tokenIterator, res);

            Assert.Greater(res.Count, startingCount);
            Assert.IsTrue(res.Any(x => x.TokenType == TokenType.FOR));
        }

        [Test]
        public void Loop_WhenInfinite_ShouldBeFor()
        {
            var scriptContent = @"
loop
{
break;
}";
            var (tokens, tokenIterator) = Prepare(scriptContent);
            var startingCount = tokens.Count;
            var res = new List<Token>();

            AdvanceGather(tokenIterator, res);

            Assert.Greater(res.Count, startingCount);
            Assert.IsTrue(res.Any(x => x.TokenType == TokenType.FOR));
        }

        [Test]
        public void Loop_WhenArr_ShouldBeFor()
        {
            var scriptContent = @"
var arr = [1,2,3];

loop arr
{
    print(item);
}";
            var (tokens, tokenIterator) = Prepare(scriptContent);
            var startingCount = tokens.Count;
            var res = new List<Token>();

            AdvanceGather(tokenIterator, res);

            Assert.Greater(res.Count, startingCount);
            Assert.IsTrue(res.Any(x => x.TokenType == TokenType.FOR));
        }

        [Test]
        public void For_WhenArr_ShouldBeFor()
        {
            var scriptContent = @"
var arr = [1,2,3];

if(arr)
{
    var count = arr.Count();
    if(count > 0)
    {
        var i = 0;
        var item = arr[i];
        for(; i < count; i += 1)
        {
            item = arr[i];
            print(item);
        }
    }
}";
            var (tokens, tokenIterator) = Prepare(scriptContent);
            var startingCount = tokens.Count;
            var res = new List<Token>();

            AdvanceGather(tokenIterator, res);

            Assert.Greater(res.Count, startingCount);
            Assert.IsTrue(res.Any(x => x.TokenType == TokenType.FOR));
        }

        [Test]
        public void PlusEqual_WhenDesugar_ShouldAssignAndExpPlus()
        {
            var scriptContent = @"
var a = 1;
a +=1;
print(a);";
            var (tokens, tokenIterator) = Prepare(scriptContent);
            var startingCount = tokens.Count;
            var res = new List<Token>();

            AdvanceGather(tokenIterator, res);

            Assert.Greater(res.Count, startingCount);
            Assert.IsFalse(res.Any(x => x.TokenType == TokenType.PLUS_EQUAL));
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

            TestContext.Write(string.Join(Environment.NewLine, list.Select(x => $"{x.TokenType}-{x.Lexeme}-{x.Literal}")));
        }
    }
}