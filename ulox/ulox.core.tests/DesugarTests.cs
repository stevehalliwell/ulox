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
            var (_, tokenIterator, _) = Prepare(scriptContent);
            var res = new List<Token>();

            AdvanceGather(tokenIterator, res);

            Assert.AreEqual(1, res.Count);
        }

        [Test]
        public void DeclareVar_WhenDesugar_ShouldHaveSame()
        {
            var scriptContent = @"var s = 1;";
            var (tokens, tokenIterator, _) = Prepare(scriptContent);
            var startingCount = tokens.Count;
            var res = new List<Token>();

            AdvanceGather(tokenIterator, res);

            Assert.AreEqual(res.Count, startingCount);
        }

        [Test]
        public void StringInterp_WhenDesugar_ShouldHaveMoreTokens()
        {
            var scriptContent = @"var s = ""Hi, {3}"";";
            var (tokens, tokenIterator, _) = Prepare(scriptContent);
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
            var (tokens, tokenIterator, _) = Prepare(scriptContent);
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
            var (tokens, tokenIterator, _) = Prepare(scriptContent);
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
{
var arr = [1,2,3];

loop arr
{
    print(item);
}
}";
            var (tokens, tokenIterator, _) = Prepare(scriptContent);
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
{
var arr = [1,2,3];
{
    var arr0 = arr;
    if(arr)
    {
        var count = countof arr;
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
    }
}
}";
            var (tokens, tokenIterator, _) = Prepare(scriptContent);
            var startingCount = tokens.Count;
            var res = new List<Token>();

            AdvanceGather(tokenIterator, res);

            Assert.Greater(res.Count, startingCount);
            Assert.IsTrue(res.Any(x => x.TokenType == TokenType.FOR));
        }

        [Test]
        public void Loop_WhenExpArr_ShouldBeFor()
        {
            var scriptContent = @"
{
var dyn = { arr = [1,2,3], };

loop dyn.arr
{
    print(item);
}
}";
            var (tokens, tokenIterator, _) = Prepare(scriptContent);
            var startingCount = tokens.Count;
            var res = new List<Token>();

            AdvanceGather(tokenIterator, res);

            Assert.Greater(res.Count, startingCount);
            Assert.IsTrue(res.Any(x => x.TokenType == TokenType.FOR));
        }

        [Test]
        public void For_WhenExpArr_ShouldBeFor()
        {
            var scriptContent = @"
{
var dyn = { arr = [1,2,3], };
{
    var arr0 = dyn.arr;
    if(arr0)
    {
        var count = countof arr0;
        if(count > 0)
        {
            var i = 0;
            var item = arr0[i];
            for(; i < count; i += 1)
            {
                item = arr0[i];
                print(item);
            }
        }
    }
}
}";
            var (tokens, tokenIterator, _) = Prepare(scriptContent);
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
            var (tokens, tokenIterator, _) = Prepare(scriptContent);
            var startingCount = tokens.Count;
            var res = new List<Token>();

            AdvanceGather(tokenIterator, res);

            Assert.Greater(res.Count, startingCount);
            Assert.IsFalse(res.Any(x => x.TokenType == TokenType.PLUS_EQUAL));
        }

        [Test]
        public void EmptyList_WhenDesugar_ShouldIdentList()
        {
            var scriptContent = @"
var a = [];
";
            var (tokens, tokenIterator, _) = Prepare(scriptContent);
            var startingCount = tokens.Count;
            var res = new List<Token>();

            AdvanceGather(tokenIterator, res);

            Assert.Greater(res.Count, startingCount);
            Assert.IsTrue(res.Any(x => x.Literal as string == "List"));
        }

        [Test]
        public void EmptyMap_WhenDesugar_ShouldIdentMap()
        {
            var scriptContent = @"
var a = [:];
";
            var (tokens, tokenIterator, _) = Prepare(scriptContent);
            var startingCount = tokens.Count;
            var res = new List<Token>();

            AdvanceGather(tokenIterator, res);

            Assert.IsTrue(res.Any(x => x.Literal as string == "Map"));
        }

        [Test]
        public void EmptyDynamic_WhenDesugar_ShouldIdentDynamic()
        {
            var scriptContent = @"
var a = {=};
";
            var (tokens, tokenIterator, _) = Prepare(scriptContent);
            var startingCount = tokens.Count;
            var res = new List<Token>();

            AdvanceGather(tokenIterator, res);

            Assert.IsTrue(res.Any(x => x.Literal as string == "Dynamic"));
        }

        [Test]
        public void Init_WhenDesugar_ShouldSelfAssign()
        {
            var scriptContent = @"
class Foo
{
    var bar, bat, baz;
    init(bar, baz, goo){}
}
";
            var (tokens, tokenIterator, context) = Prepare(scriptContent);
            var startingCount = tokens.Count;
            var res = new List<Token>();
            context.IsInClassValue = true;
            context.ClassFieldNames.Add("bar");
            context.ClassFieldNames.Add("bat");
            context.ClassFieldNames.Add("baz");

            AdvanceGather(tokenIterator, res);

            Assert.Greater(res.Count, startingCount);
        }

        [Test]
        public void Soa_FooAB_ClassWith2Arrays()
        {
            var scriptContent = @"
soa FooSoa
{
    Foo,
}
";
            var (tokens, tokenIterator, context) = Prepare(scriptContent);
            var startingCount = tokens.Count;
            var res = new List<Token>();
            var fooType = new TypeInfoEntry("Foo", UserType.Class);
            fooType.AddField("a");
            fooType.AddField("b");
            context.TypeInfo.AddType(fooType);

            AdvanceGather(tokenIterator, res);

            Assert.Greater(res.Count, startingCount);
            Assert.IsTrue(res.Any(x => x.TokenType == TokenType.CLASS));
            Assert.IsTrue(res.Any(x => x.Literal as string == "FooSoa"));
            Assert.IsTrue(res.Any(x => x.Literal as string == "a"));
            Assert.IsTrue(res.Any(x => x.Literal as string == "b"));
            Assert.IsTrue(res.Any(x => x.Literal as string == "Count"));
        }

        private static (List<Token> tokens, TokenIterator tokenIterator, DummyContext context) Prepare(string scriptContent)
        {
            var scanner = new Scanner();
            var script = new Script("test", scriptContent);
            var tokenisedScript = scanner.Scan(script);
            var context = new DummyContext();
            var tokenIterator = new TokenIterator(script, tokenisedScript, context);
            return (tokenisedScript.Tokens, tokenIterator, context);
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

            TestContext.Write(string.Join(Environment.NewLine, list.Select(x => $"{x.TokenType}-{x.Literal as string}-{x.Literal}")));
        }

        public class DummyContext : ICompilerDesugarContext
        {
            public bool IsInClassValue { get; set; }
            public HashSet<string> ClassFieldNames { get; set; } = new HashSet<string>();

            public TypeInfo TypeInfo { get; set; } = new ();

            private int _uniqueNameCount = 0;

            public bool DoesCurrentClassHaveMatchingField(string x)
            {
                return ClassFieldNames.Contains(x);
            }

            public bool IsInClass()
            {
                return IsInClassValue;
            }

            public string UniqueLocalName(string prefix)
            {
                return $"{prefix}{_uniqueNameCount++}";
            }
        }
    }
}