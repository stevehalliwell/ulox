using NUnit.Framework;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

namespace ULox.Tests
{
    [TestFixture]
    public class XMLSerialisationTests : EngineTestBase
    {
        private const string SimpleTestObject = @"
class T
{
    var a = 1, b = 2, c = 3;
}

var obj = T();
obj.a = T();
obj.b = 4;
obj.c = 5;";
        private const string SimpleSBTestObject = @"a
  a:1
  b:2
  c:3
b:4
c:5";
        private const string SimpleTestObjectResult = @"<root>
  <a>
    <a>1</a>
    <b>2</b>
    <c>3</c>
  </a>
  <b>4</b>
  <c>5</c>
</root>";

        [Test]
        public void Serialise_WhenGivenKnownObject_ShouldReturnExpectedOutput()
        {
            var scriptString = SimpleTestObject;

            var expected = SimpleTestObjectResult;
            var result = "error";

            testEngine.Run(scriptString);
            var obj = testEngine.MyEngine.Context.VM.GetGlobal(new HashedString("obj"));
            var xml = new XmlValueHeirarchyWriter();
            var xmlWalker = new ValueHeirarchyWalker(xml);
            xmlWalker.Walk(obj);
            result = xml.GetString();

            StringAssert.Contains(Regex.Replace(expected, @"\s+", " "), Regex.Replace(result, @"\s+", " "));
        }

        [Test]
        public void Deserialise_WhenGivenKnownString_ShouldReturnExpectedObject()
        {
            var xml = SimpleTestObjectResult;

            var reader = new StringReader(xml);
            var xmlReader = XmlReader.Create(reader, new XmlReaderSettings() { IgnoreComments = true, IgnoreWhitespace = true });
            var creator = new XmlDocValueHeirarchyTraverser(new ValueObjectBuilder(ValueObjectBuilder.ObjectType.Object), xmlReader);
            creator.Process();
            var obj = creator.Finish();

            Assert.AreEqual(ValueType.Instance, obj.type);

            var testWriter = new StringBuilderValueHeirarchyWriter();
            var testObjWalker = new ValueHeirarchyWalker(testWriter);
            testObjWalker.Walk(obj);
            var resultString = testWriter.GetString();
            var expectedWalkResult = SimpleSBTestObject;

            StringAssert.Contains(Regex.Replace(expectedWalkResult, @"\s+", " "), Regex.Replace(resultString, @"\s+", " "));
            Assert.IsTrue(obj.val.asInstance.HasField(new HashedString("a")));
            Assert.IsTrue(obj.val.asInstance.HasField(new HashedString("b")));
            Assert.IsTrue(obj.val.asInstance.HasField(new HashedString("c")));
        }

        [Test]
        public void DeserialiseSerialise_WhenGivenKnownString_ShouldReturnExpectedObject()
        {
            var xml = SimpleTestObjectResult;

            var reader = new StringReader(xml);
            var xmlReader = XmlReader.Create(reader, new XmlReaderSettings() { IgnoreComments = true, IgnoreWhitespace = true });
            var creator = new XmlDocValueHeirarchyTraverser(new ValueObjectBuilder(ValueObjectBuilder.ObjectType.Object), xmlReader);
            creator.Process();
            var obj = creator.Finish();

            Assert.AreEqual(ValueType.Instance, obj.type);

            var xmlWriter = new XmlValueHeirarchyWriter();
            var xmlWalker = new ValueHeirarchyWalker(xmlWriter);
            xmlWalker.Walk(obj);
            var resultString = xmlWriter.GetString();

            StringAssert.Contains(Regex.Replace(xml, @"\s+", " "), Regex.Replace(resultString, @"\s+", " "));
            Assert.IsTrue(obj.val.asInstance.HasField(new HashedString("a")));
            Assert.IsTrue(obj.val.asInstance.HasField(new HashedString("b")));
            Assert.IsTrue(obj.val.asInstance.HasField(new HashedString("c")));
        }
    }
}
