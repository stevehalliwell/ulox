using NUnit.Framework;
using System;
using System.Collections.Generic;
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
                foreach (var field in v.val.asInstance.Fields.OrderBy(x => x.Key.String))
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

    public class ValueObjectBuilder
    {
        private Value _value;
        private List<(string name, ValueObjectBuilder builder)> _children = new List<(string, ValueObjectBuilder)>();

        public ValueObjectBuilder()
        {
            _value = Value.New(new InstanceInternal());
        }

        public Value Finish()
        {
            for (int i = _children.Count - 1; i >= 0; i--)
            {
                var item = _children[i];
                var childVal = item.builder.Finish();
                _value.val.asInstance.SetField(new HashedString(item.name), childVal);
            }

            return _value;
        }

        public void SetField(string v1, string v2)
        {
            _value.val.asInstance.SetField(new HashedString(v1), Value.New(v2));
        }

        public ValueObjectBuilder CreateChild(string prevNodeName)
        {
            var child = new ValueObjectBuilder();
            _children.Add((prevNodeName, child));
            return child;
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
            var valBuilder = new ValueObjectBuilder();
            ProcessLine(0, valBuilder);
            return valBuilder.Finish();
        }

        private void ProcessLine(int prevIndent, ValueObjectBuilder valBuilder)
        {
            while (_consumer.Read())
            {
                var currentLine = _consumer.CurrentLine;
                var numLeadingSpaces = currentLine.TrimStart(' ');
                var leadingSpaces = currentLine.Length - numLeadingSpaces.Length;
                var currentIndent = leadingSpaces != 0 ? leadingSpaces / 2 : 0;

                if (currentIndent <= prevIndent)
                    return;

                var trimmed = numLeadingSpaces.Trim();
                var split = trimmed.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

                if(split.Length == 1)
                {
                    if (currentIndent == prevIndent + 1)
                    {
                        _consumer.Consume();
                        var childBuilder = valBuilder.CreateChild(split[0]);
                        ProcessLine(currentIndent, childBuilder);
                    }
                    else
                        throw new Exception();

                }
                else if(split.Length == 2)
                {
                    valBuilder.SetField(split[0], split[1]);
                    _consumer.Consume();
                }
            }
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

        public Value Process()
        {
            _reader.Read();
            if (_reader.Name != "root")
                return Value.Null();

            _reader.Read();
            Console.WriteLine($"{_reader.NodeType}:{_reader.Name}");
            var builder = new ValueObjectBuilder();
            ProcessNode(builder);
            return builder.Finish();
        }
        
        private void ProcessNode(ValueObjectBuilder valBuilder)
        {
            _prevNodeType = _reader.NodeType;
            _prevNodeName = _reader.Name;
            while (_reader.Read())
            {
                Console.WriteLine($"{_reader.NodeType}:{_reader.Name}");
                switch (_reader.NodeType)
                {
                case XmlNodeType.Element:
                    if(_prevNodeType == XmlNodeType.Element)
                    {
                        var childBuilder = valBuilder.CreateChild(_prevNodeName);
                        ProcessNode(childBuilder);
                    }
                    break;
                case XmlNodeType.Text:
                    valBuilder.SetField(_prevNodeName, _reader.Value);
                    break;
                case XmlNodeType.EndElement:
                    if (_prevNodeType == XmlNodeType.EndElement)
                        return;
                    break;
                default:
                    throw new Exception();
                }
                _prevNodeType = _reader.NodeType;
                _prevNodeName = _reader.Name;
            }
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
