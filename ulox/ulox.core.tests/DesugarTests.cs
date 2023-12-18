using NUnit.Framework;
using System.Collections.Generic;

namespace ULox.Core.Tests
{
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

            Assert.AreEqual(res.Count, startingCount+2);
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