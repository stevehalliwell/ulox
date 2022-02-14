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
}
