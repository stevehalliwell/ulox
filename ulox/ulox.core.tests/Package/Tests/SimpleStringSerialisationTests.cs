using NUnit.Framework;
using System.IO;
using System.Text.RegularExpressions;

namespace ULox.Tests
{
    [TestFixture]
    public class SimpleStringSerialisationTests : EngineTestBase
    {
        [Test]
        public void Serialise_WhenGivenKnownObject_ShouldReturnExpectedOutput()
        {
            var scriptString = @"
class T
{
    var a = 1, b = 2, c = 3;
}

var obj = T();
obj.a = T();
obj.b = 4;
obj.c = 5;";
            var expected = @"root
  a
    a:1
    b:2
    c:3
  b:4
  c:5";
            var result = "error";
            testEngine.Run(scriptString);
            var obj = testEngine.MyEngine.Context.VM.GetGlobal(new HashedString("obj"));
            var testWriter = new StringBuilderValueHeirarchyWriter();
            var testObjWalker = new ValueHeirarchyWalker(testWriter);
            
            testObjWalker.Walk(obj);
            result = testWriter.GetString();

            StringAssert.Contains(Regex.Replace(expected, @"\s+", " "), Regex.Replace(result, @"\s+", " "));
        }

        [Test]
        public void Deserialise_WhenGivenKnownString_ShouldReturnExpectedObject()
        {
            var fromSb = @"root
  a
    a:1
    b:2
    c:3
  b:4
  c:5";

            var reader = new StringReader(fromSb);
            var creator = new StringBuildDocValueHeirarchyTraverser(new ValueObjectBuilder(), reader);
            var obj = Value.Null();

            creator.Process();
            obj = creator.Finish();

            Assert.AreEqual(ValueType.Instance, obj.type);
            Assert.IsTrue(obj.val.asInstance.HasField(new HashedString("a")));
            Assert.IsTrue(obj.val.asInstance.HasField(new HashedString("b")));
            Assert.IsTrue(obj.val.asInstance.HasField(new HashedString("c")));
            var inner = obj.val.asInstance.GetField(new HashedString("a"));
            Assert.IsTrue(inner.val.asInstance.HasField(new HashedString("a")));
            Assert.IsTrue(inner.val.asInstance.HasField(new HashedString("b")));
            Assert.IsTrue(inner.val.asInstance.HasField(new HashedString("c")));

            var testWriter = new StringBuilderValueHeirarchyWriter();
            var testObjWalker = new ValueHeirarchyWalker(testWriter);
            testObjWalker.Walk(obj);
            var resultString = testWriter.GetString();
            var expectedWalkResult = @"root
  a
    a:1
    b:2
    c:3
  b:4
  c:5";
            StringAssert.Contains(Regex.Replace(expectedWalkResult, @"\s+", " "), Regex.Replace(resultString, @"\s+", " "));
        }

        [Test]
        public void SerialiseDeserialise_WhenGivenKnownString_ShouldReturnExpectedObject()
        {
            var fromSb = @"root
  a
    a:1
    b:2
    c:3
  b:4
  c:5";
            var reader = new StringReader(fromSb);
            var creator = new StringBuildDocValueHeirarchyTraverser(new ValueObjectBuilder(), reader);
            var obj = Value.Null();
            creator.Process();
            obj = creator.Finish();
            var testWriter = new StringBuilderValueHeirarchyWriter();
            var testObjWalker = new ValueHeirarchyWalker(testWriter);
            
            testObjWalker.Walk(obj);
            var resultString = testWriter.GetString();

            StringAssert.Contains(Regex.Replace(fromSb, @"\s+", " "), Regex.Replace(resultString, @"\s+", " "));
            Assert.IsTrue(obj.val.asInstance.HasField(new HashedString("a")));
            Assert.IsTrue(obj.val.asInstance.HasField(new HashedString("b")));
            Assert.IsTrue(obj.val.asInstance.HasField(new HashedString("c")));
        }
    }
}
