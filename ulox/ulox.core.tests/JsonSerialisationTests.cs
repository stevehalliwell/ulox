using NUnit.Framework;
using System.IO;
using System.Text.RegularExpressions;
using ULox;

namespace ulox.core.tests
{
    [TestFixture]
    public class JsonSerialisationTests : EngineTestBase
    {
        public const string UloxJsonExpectedResult = @"{
  ""a"": {
    ""a"": [
      ""a"",
      ""b"",
      ""c""
    ],
    ""b"": 2.0,
    ""c"": 3.0
  },
  ""b"": 4.0,
  ""c"": true
}";
        public const string BiggerTestObjectString = @"
class T
{
    var a = 1, b = 2, c = 3;
}

var objInList = T();
objInList.b = ""pretty deep in here"";

var l = [];
l.Add(""a"");
l.Add(""b"");
l.Add(objInList);

var obj = T();
obj.a = T();
obj.b = 4;
obj.c = true;
obj.a.a = l;";
        public const string BiggerSBExpectedResult = @"{
  ""a"": {
    ""a"": [
      ""a"",
      ""b"",
      {
        ""a"": 1.0,
        ""b"": ""pretty deep in here"",
        ""c"": 3.0
      }
    ],
    ""b"": 2.0,
    ""c"": 3.0
  },
  ""b"": 4.0,
  ""c"": true
}";

        [Test]
        public void Serialise_WhenGivenKnownObject_ShouldReturnExpectedOutput()
        {
            var scriptString = SimpleStringSerialisationTests.UloxTestObjectString;
            var expected = UloxJsonExpectedResult;
            var result = "error";
            testEngine.Run(scriptString);
            var obj = testEngine.MyEngine.Context.VM.GetGlobal(new HashedString("obj"));
            var jsonWriter = new JsonValueHeirarchyWriter();
            var walker = new ValueHeirarchyWalker(jsonWriter);

            walker.Walk(obj);
            result = jsonWriter.GetString();

            StringAssert.Contains(Regex.Replace(expected, @"\s+", " "), Regex.Replace(result, @"\s+", " "));
        }

        [Test]
        public void Serialise_WhenGivenNull_ShouldReturnExpectedOutput()
        {
            var scriptString = @"var obj = null;";
            var expected = string.Empty;
            var result = "error";
            testEngine.Run(scriptString);
            var obj = testEngine.MyEngine.Context.VM.GetGlobal(new HashedString("obj"));
            var jsonWriter = new JsonValueHeirarchyWriter();
            var walker = new ValueHeirarchyWalker(jsonWriter);

            walker.Walk(obj);
            result = jsonWriter.GetString();

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void Serialise_WhenGivenEmptyObject_ShouldReturnExpectedOutput()
        {
            var scriptString = @"var obj = {:};";
            var expected = string.Empty;
            var result = "error";
            testEngine.Run(scriptString);
            var obj = testEngine.MyEngine.Context.VM.GetGlobal(new HashedString("obj"));
            var jsonWriter = new JsonValueHeirarchyWriter();
            var walker = new ValueHeirarchyWalker(jsonWriter);

            walker.Walk(obj);
            result = jsonWriter.GetString();

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void Serialise_WhenGiveBiggerObject_ShouldReturnExpectedOutput()
        {
            var scriptString = BiggerTestObjectString;
            var expected = BiggerSBExpectedResult;
            var result = "error";
            testEngine.Run(scriptString);
            var obj = testEngine.MyEngine.Context.VM.GetGlobal(new HashedString("obj"));
            var jsonWriter = new JsonValueHeirarchyWriter();
            var walker = new ValueHeirarchyWalker(jsonWriter);

            walker.Walk(obj);
            result = jsonWriter.GetString();

            StringAssert.Contains(Regex.Replace(expected, @"\s+", " "), Regex.Replace(result, @"\s+", " "));
        }

        [Test]
        public void Deserialise_WhenGivenKnownString_ShouldReturnExpectedObject()
        {
            var jsonString = UloxJsonExpectedResult;
            var reader = new StringReader(jsonString);
            var creator = new JsonDocValueHeirarchyTraverser(new ValueObjectBuilder(ValueObjectBuilder.ObjectType.Object), reader);
            creator.Process();
            var obj = creator.Finish();

            Assert.AreEqual(ValueType.Instance, obj.type);

            var testWriter = new StringBuilderValueHeirarchyWriter();
            var testObjWalker = new ValueHeirarchyWalker(testWriter);
            testObjWalker.Walk(obj);
            var resultString = testWriter.GetString();
            var expectedWalkResult = SimpleStringSerialisationTests.UloxSBExpectedResult;
            StringAssert.Contains(Regex.Replace(expectedWalkResult, @"\s+", " "), Regex.Replace(resultString, @"\s+", " "));
            Assert.IsTrue(obj.val.asInstance.HasField(new HashedString("a")));
            Assert.IsTrue(obj.val.asInstance.HasField(new HashedString("b")));
            Assert.IsTrue(obj.val.asInstance.HasField(new HashedString("c")));
        }

        [Test]
        public void DeserialiseSerialise_WhenGivenKnownString_ShouldReturnExpectedObject()
        {
            var json = UloxJsonExpectedResult;
            var reader = new StringReader(json);
            var creator = new JsonDocValueHeirarchyTraverser(new ValueObjectBuilder(ValueObjectBuilder.ObjectType.Object), reader);
            creator.Process();
            var obj = creator.Finish();

            var jsonWriter = new JsonValueHeirarchyWriter();
            var walker = new ValueHeirarchyWalker(jsonWriter);
            walker.Walk(obj);
            var result = jsonWriter.GetString();

            StringAssert.Contains(Regex.Replace(json, @"\s+", " "), Regex.Replace(result, @"\s+", " "));
            Assert.IsTrue(obj.val.asInstance.HasField(new HashedString("a")));
            Assert.IsTrue(obj.val.asInstance.HasField(new HashedString("b")));
            Assert.IsTrue(obj.val.asInstance.HasField(new HashedString("c")));
        }

        [Test]
        public void SerialiseViaLibrary_WhenGivenKnownObject_ShouldReturnExpectedOutput()
        {
            const string Expected = @"{ 
    ""a"": 1.0, 
    ""b"": 2.0, 
    ""c"": 3.0 
}";

            testEngine.Run(@"
class T { var a = 1, b = 2, c = 3;}

var obj = T();
var res = Serialise.ToJson(obj);
print(res);
");
            StringAssert.Contains(Regex.Replace(Expected, @"\s+", " "), Regex.Replace(testEngine.InterpreterResult, @"\s+", " "));
        }

        [Test]
        public void DeserialiseViaLibrary_WhenGivenKnownObject_ShouldReturnExpectedOutput()
        {
            testEngine.Run(@"
var jsonString = ""{ \""a\"": 1.0,  \""b\"": 2.0,  \""c\"": 3.0 }"";
var res = Serialise.FromJson(jsonString);
print(res.a);
print(res.b);
print(res.c);
");

            Assert.AreEqual("123", testEngine.InterpreterResult);
        }

        [Test]
        public void DeserialiseViaLibrary_WhenGivenKnownArray_ShouldReturnExpectedOutput()
        {
            testEngine.Run(@"
var jsonString = ""{ \""a\"": [ 1.0, 2.0, 3.0 ] }"";
var res = Serialise.FromJson(jsonString);
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }
    }
}
