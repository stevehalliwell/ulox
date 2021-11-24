using NUnit.Framework;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using UnityEngine;

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

            var obj = testEngine.Vm.GetGlobal(new HashedString("obj"));
            var testWriter = new StringBuilderValueHeirarchyWriter();
            var testObjWalker = new ValueHeirarchyWalker(testWriter);
            testObjWalker.Walk(obj);
            var res = testWriter.GetString();
            Debug.Log(res);

            var xml = new XmlValueHeirarchyWriter();
            var xmlWalker = new ValueHeirarchyWalker(xml);
            xmlWalker.Walk(obj);
            res = xml.GetString();
            Debug.Log(res);
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
            var obj = testEngine.Vm.GetGlobal(new HashedString("obj"));
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
            var obj = testEngine.Vm.GetGlobal(new HashedString("obj"));
            var xml = new XmlValueHeirarchyWriter();
            var xmlWalker = new ValueHeirarchyWalker(xml);
            xmlWalker.Walk(obj);
            result = xml.GetString();

            StringAssert.Contains(Regex.Replace(expected, @"\s+", " "), Regex.Replace(result, @"\s+", " "));
        }
    }
}
