using Newtonsoft.Json;
using NUnit.Framework;
using System.IO;
using System.Text.RegularExpressions;

namespace ULox.Tests
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
    ""b"": ""2"",
    ""c"": ""3""
  },
  ""b"": ""4"",
  ""c"": ""5""
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
        public void Deserialise_WhenGivenKnownString_ShouldReturnExpectedObject()
        {
            var xml = UloxJsonExpectedResult;
            var reader = new StringReader(xml);
            var jsonReader = new JsonTextReader(reader);
            var creator = new JsonDocValueHeirarchyTraverser(new ValueObjectBuilder(ValueObjectBuilder.ObjectType.Object), jsonReader);
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
            var jsonReader = new JsonTextReader(reader);
            var creator = new JsonDocValueHeirarchyTraverser(new ValueObjectBuilder(ValueObjectBuilder.ObjectType.Object), jsonReader);
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
    }
}
