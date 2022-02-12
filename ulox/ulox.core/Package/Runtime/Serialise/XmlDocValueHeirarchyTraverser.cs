using Newtonsoft.Json;
using System;
using System.Xml;

namespace ULox
{
    public class XmlDocValueHeirarchyTraverser : DocValueHeirarchyTraverser
    {
        private XmlReader _reader;

        public XmlDocValueHeirarchyTraverser(
            IValueObjectBuilder valBuilder,
            XmlReader reader)
            : base(valBuilder)
        {
            _reader = reader;
        }

        public override void Prepare()
        {
            _reader.Read();
            if (_reader.Name != "root")
                return;

            _reader.Read();
        }

        protected override void ProcessNode()
        {
            var prevNodeType = _reader.NodeType;
            var prevNodeName = _reader.Name;
            while (_reader.Read())
            {
                switch (_reader.NodeType)
                {
                case XmlNodeType.Element:
                    if (prevNodeType == XmlNodeType.Element)
                    {
                        StartChild(prevNodeName);
                        ProcessNode();
                    }
                    break;
                case XmlNodeType.Text:
                    var name = prevNodeName;
                    var val = _reader.Value;
                    Field(name, val);
                    break;
                case XmlNodeType.EndElement:
                    if (prevNodeType == XmlNodeType.EndElement)
                    {
                        EndChild();
                        return;
                    }
                    break;
                default:
                    throw new Exception();
                }
                prevNodeType = _reader.NodeType;
                prevNodeName = _reader.Name;
            }
            EndChild();
        }
    }
    public class JsonDocValueHeirarchyTraverser : DocValueHeirarchyTraverser
    {
        private JsonTextReader _reader;

        public JsonDocValueHeirarchyTraverser(
            IValueObjectBuilder valBuilder,
            JsonTextReader reader)
            : base(valBuilder)
        {
            _reader = reader;
        }

        public override void Prepare()
        {
            _reader.Read();
            if (_reader.TokenType != JsonToken.StartObject)
                return;
        }

        protected override void ProcessNode()
        {
            var prevPropName = string.Empty;
            while (_reader.Read())
            {
                switch (_reader.TokenType)
                {
                case JsonToken.StartObject:
                    StartChild(prevPropName);
                    ProcessNode();
                    break;
                case JsonToken.PropertyName:
                    prevPropName = (string)_reader.Value;
                    break;
                case JsonToken.String:
                    Field(prevPropName, (string)_reader.Value);
                    break;
                case JsonToken.EndObject:
                    EndChild();
                    return;
                default:
                    throw new Exception();
                }
            }
            EndChild();
        }
    }
}
