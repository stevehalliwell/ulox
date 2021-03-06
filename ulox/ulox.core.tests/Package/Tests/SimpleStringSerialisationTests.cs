using NUnit.Framework;
using System.IO;
using System.Text.RegularExpressions;

namespace ULox.Tests
{
    [TestFixture]
    public class SimpleStringSerialisationTests : EngineTestBase
    {
        public const string UloxTestObjectString = @"
class T
{
    var a = 1, b = 2, c = 3;
}

var l = [];
l.Add(""a"");
l.Add(""b"");
l.Add(""c"");

var obj = T();
obj.a = T();
obj.b = 4;
obj.c = true;
obj.a.a = l;";
        public const string UloxSBExpectedResult = @"a
  a:[
  a
  b
  c
  ]
  b:2
  c:3
b:4
c:True
";

        [Test]
        public void Serialise_WhenGivenKnownObject_ShouldReturnExpectedOutput()
        {
            var scriptString = UloxTestObjectString;
            var expected = UloxSBExpectedResult;
            var result = "error";
            testEngine.Run(scriptString);
            var obj = testEngine.MyEngine.Context.VM.GetGlobal(new HashedString("obj"));
            var testWriter = new StringBuilderValueHeirarchyWriter();
            var testObjWalker = new ValueHeirarchyWalker(testWriter);
            
            testObjWalker.Walk(obj);
            result = testWriter.GetString();

            StringAssert.Contains(Regex.Replace(expected, @"\s+", " "), Regex.Replace(result, @"\s+", " "));
        }
    }
}
