using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace ULox.Tests
{
    public interface IValueHeirarchyWriter
    {
        void WriteNameAndValue(HashedString name, Value v);

        void StartElement(HashedString name, Value v);

        void EndElement(HashedString name, Value v);
    }

    public class ValueHeirarchyWalker
    {
        private IValueHeirarchyWriter _writer;

        public ValueHeirarchyWalker(IValueHeirarchyWriter writer)
        {
            _writer = writer;
        }

        public void Walk(Value v)
        {
            WalkElement(new HashedString("root"), v);
        }

        private void WalkElement(HashedString name, Value v)
        {
            switch (v.type)
            {
            case ValueType.Instance:
                _writer.StartElement(name, v);
                foreach (var field in v.val.asInstance.Fields)
                {
                    WalkElement(field.Key, field.Value);
                }
                _writer.EndElement(name, v);
                break;

            default:
                _writer.WriteNameAndValue(name, v);
                break;
            }
        }
    }

    public class StringBuilderValueHeirarchyWriter : IValueHeirarchyWriter
    {
        private StringBuilder _sb = new StringBuilder();
        private int _indent;

        public string GetString() => _sb.ToString();

        public void EndElement(HashedString name, Value v) => _indent--;

        private void AppendIndent()
        {
            _sb.Append(string.Concat(Enumerable.Repeat("  ", _indent)));
        }

        public void WriteNameAndValue(HashedString name, Value v)
        {
            AppendIndent();
            _sb.AppendLine($"{name.String}:{v}");
        }

        public void StartElement(HashedString name, Value v)
        {
            AppendIndent();
            _sb.AppendLine(name.String);
            _indent++;
        }
    }

    public class XmlValueHeirarchyWriter : IValueHeirarchyWriter
    {
        public XmlWriter xmlWriter;
        public StringBuilder sb;

        public XmlValueHeirarchyWriter()
        {
            sb = new StringBuilder();
            var xmlsettings = new XmlWriterSettings();
            xmlsettings.Indent = true;
            xmlsettings.Encoding = Encoding.UTF8;
            xmlWriter = XmlWriter.Create(sb, xmlsettings);
            xmlWriter.WriteStartDocument();
        }

        public string GetString()
        {
            xmlWriter.WriteEndDocument();
            xmlWriter.Flush();
            xmlWriter.Close();
            return sb.ToString();
        }

        public void WriteNameAndValue(HashedString name, Value v)
        {
            xmlWriter.WriteElementString(name.String, v.ToString());
        }

        public void StartElement(HashedString name, Value v)
        {
            xmlWriter.WriteStartElement(name.String);
        }

        public void EndElement(HashedString name, Value v)
        {
            xmlWriter.WriteEndElement();
        }
    }

    public class StringBuildDocValueHeirarchyCreator
    {
        private class StringReaderConsumer
        {
            private StringReader _reader;
            public string CurrentLine { get; private set; }
            private bool _consumed = true;

            public StringReaderConsumer(StringReader reader)
            {
                _reader = reader;
            }

            public bool Read()
            {
                if (_consumed)
                {
                    CurrentLine = _reader.ReadLine();
                    _consumed = false;
                }

                return CurrentLine != null;
            }

            public void Consume()
            {
                _consumed = true;
            }
        }

        private StringReaderConsumer _consumer;


        public StringBuildDocValueHeirarchyCreator(StringReader reader)
        {
            _consumer = new StringReaderConsumer(reader);
        }

        public Value Process()
        {
            _consumer.Read();
            _consumer.Consume();
            return ProcessLine(0);
        }

        private Value ProcessLine(int prevIndent)
        {
            var curVal = Value.New(new InstanceInternal());

            while (_consumer.Read())
            {
                var currentLine = _consumer.CurrentLine;
                var numLeadingSpaces = currentLine.TrimStart(' ');
                var leadingSpaces = currentLine.Length - numLeadingSpaces.Length;
                var currentIndent = leadingSpaces != 0 ? leadingSpaces / 2 : 0;

                if (currentIndent <= prevIndent)
                    return curVal;

                var trimmed = numLeadingSpaces.Trim();
                var split = trimmed.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

                if(split.Length == 1)
                {
                    if (currentIndent == prevIndent + 1)
                    {
                        _consumer.Consume();
                        curVal.val.asInstance.SetField(new HashedString(split[0]), ProcessLine(currentIndent));
                    }
                    else
                        throw new Exception();

                }
                else if(split.Length == 2)
                {
                    curVal.val.asInstance.SetField(new HashedString(split[0]), Value.New(split[1]));
                    _consumer.Consume();
                }
            }

            return curVal;
        }
    }

    public class XmlDocValueHeirarchyCreator
    {
        private XmlReader _reader;
        private XmlNodeType _prevNodeType;
        private string _prevNodeName;

        public XmlDocValueHeirarchyCreator(XmlReader reader)
        {
            _reader = reader;
        }


        //todo prime root and curval with root node, then call a processnode function,
        //  takes cur, if encounters prevNode and node element, calls processnode with new cur as arg

        public Value Process()
        {
            _reader.Read();
            if (_reader.Name != "root")
                return Value.Null();

            _reader.Read();
            Console.WriteLine($"{_reader.NodeType}:{_reader.Name}");
            return ProcessNode();
        }
        
        private Value ProcessNode()
        {
            var curVal = Value.New(new InstanceInternal());

            _prevNodeType = _reader.NodeType;
            _prevNodeName = _reader.Name;
            while (_reader.Read())
            {
                Console.WriteLine($"{_reader.NodeType}:{_reader.Name}");
                switch (_reader.NodeType)
                {
                case XmlNodeType.None:
                    break;
                case XmlNodeType.Element:
                    if(_prevNodeType == XmlNodeType.Element)
                    {
                        curVal.val.asInstance.SetField(new HashedString(_prevNodeName), ProcessNode());
                    }
                    break;
                case XmlNodeType.Attribute:
                    break;
                case XmlNodeType.Text:
                    curVal.val.asInstance.SetField(new HashedString(_prevNodeName), Value.New(_reader.Value));
                    break;
                case XmlNodeType.CDATA:
                    break;
                case XmlNodeType.EntityReference:
                    break;
                case XmlNodeType.Entity:
                    break;
                case XmlNodeType.ProcessingInstruction:
                    break;
                case XmlNodeType.Comment:
                    break;
                case XmlNodeType.Document:
                    break;
                case XmlNodeType.DocumentType:
                    break;
                case XmlNodeType.DocumentFragment:
                    break;
                case XmlNodeType.Notation:
                    break;
                case XmlNodeType.Whitespace:
                    break;
                case XmlNodeType.SignificantWhitespace:
                    break;
                case XmlNodeType.EndElement:
                    if (_prevNodeType == XmlNodeType.EndElement)
                        return curVal;
                    break;
                case XmlNodeType.EndEntity:
                    break;
                case XmlNodeType.XmlDeclaration:
                    break;
                default:
                    break;
                }
                _prevNodeType = _reader.NodeType;
                _prevNodeName = _reader.Name;
            }

            return curVal;
        }
    }

    [TestFixture]
    public class ObjectSerialisationTests : EngineTestBase
    {
        [Test]
        public void Serialise()
        {
            testEngine.Run(@"
class T
{
    var a = 1, b = 2, c = 3;
}

var obj = T();
obj.a = T();");

            var obj = testEngine.MyEngine.Context.VM.GetGlobal(new HashedString("obj"));
            var testWriter = new StringBuilderValueHeirarchyWriter();
            var testObjWalker = new ValueHeirarchyWalker(testWriter);
            testObjWalker.Walk(obj);
            var res = testWriter.GetString();

            var xml = new XmlValueHeirarchyWriter();
            var xmlWalker = new ValueHeirarchyWalker(xml);
            xmlWalker.Walk(obj);
            res = xml.GetString();
        }

        [Test]
        public void StringBuilderSerialise_WhenGivenKnownObject_ShouldReturnExpectedOutput()
        {
            var scriptString = @"
class T
{
    var a = 1, b = 2, c = 3;
}

var obj = T();
obj.a = T();";

            var expected = @"root
  a
    a:1
    b:2
    c:3
  b:2
  c:3";
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
        public void XMLSerialise_WhenGivenKnownObject_ShouldReturnExpectedOutput()
        {
            var scriptString = @"
class T
{
    var a = 1, b = 2, c = 3;
}

var obj = T();
obj.a = T();";

            var expected = @"<root>
  <a>
    <a>1</a>
    <b>2</b>
    <c>3</c>
  </a>
  <b>2</b>
  <c>3</c>
</root>";
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
        public void XMLDeserialise_WhenGivenKnownString_ShouldReturnExpectedObject()
        {
            var xml = @"<root>
  <a>
    <a>1</a>
    <b>2</b>
    <c>3</c>
  </a>
  <b>2</b>
  <c>3</c>
</root>";

            var reader = new StringReader(xml);
            var xmlReader = XmlReader.Create(reader, new XmlReaderSettings() { IgnoreComments = true, IgnoreWhitespace = true});
            var creator = new XmlDocValueHeirarchyCreator(xmlReader);
            var obj = Value.Null();

            obj = creator.Process();

            Assert.AreEqual(ValueType.Instance, obj.type);

            var testWriter = new StringBuilderValueHeirarchyWriter();
            var testObjWalker = new ValueHeirarchyWalker(testWriter);
            testObjWalker.Walk(obj);
            var resultString = testWriter.GetString(); 
            var expectedWalkResult = @"root
  a
    a:1
    b:2
    c:3
  b:2
  c:3";

            StringAssert.Contains(Regex.Replace(expectedWalkResult, @"\s+", " "), Regex.Replace(resultString, @"\s+", " "));
            Assert.IsTrue(obj.val.asInstance.HasField(new HashedString("a")));
            Assert.IsTrue(obj.val.asInstance.HasField(new HashedString("b")));
            Assert.IsTrue(obj.val.asInstance.HasField(new HashedString("c")));
        }

        [Test]
        public void StringBuilderDeserialise_WhenGivenKnownString_ShouldReturnExpectedObject()
        {
            var sbOut = @"root
  a
    a:1
    b:2
    c:3
  b:2
  c:3";

            var reader = new StringReader(sbOut);
            var creator = new StringBuildDocValueHeirarchyCreator(reader);
            var obj = Value.Null();

            obj = creator.Process();

            Assert.AreEqual(ValueType.Instance, obj.type);

            var testWriter = new StringBuilderValueHeirarchyWriter();
            var testObjWalker = new ValueHeirarchyWalker(testWriter);
            testObjWalker.Walk(obj);
            var resultString = testWriter.GetString();
            var expectedWalkResult = @"root
  a
    a:1
    b:2
    c:3
  b:2
  c:3";
            StringAssert.Contains(Regex.Replace(expectedWalkResult, @"\s+", " "), Regex.Replace(resultString, @"\s+", " "));
            Assert.IsTrue(obj.val.asInstance.HasField(new HashedString("a")));
            Assert.IsTrue(obj.val.asInstance.HasField(new HashedString("b")));
            Assert.IsTrue(obj.val.asInstance.HasField(new HashedString("c")));
        }
    }
}
