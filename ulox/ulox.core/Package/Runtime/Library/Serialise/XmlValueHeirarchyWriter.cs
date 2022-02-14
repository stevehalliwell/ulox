using System.Text;
using System.Xml;

namespace ULox
{
    public class XmlValueHeirarchyWriter : IValueHeirarchyWriter
    {
        private readonly XmlWriter _xmlWriter;
        private readonly StringBuilder _sb;
        private bool _hasStarted = false;

        public XmlValueHeirarchyWriter()
        {
            _sb = new StringBuilder();
            var xmlsettings = new XmlWriterSettings();
            xmlsettings.Indent = true;
            xmlsettings.Encoding = Encoding.UTF8;
            _xmlWriter = XmlWriter.Create(_sb, xmlsettings);
            _xmlWriter.WriteStartDocument();
        }

        public string GetString()
        {
            _xmlWriter.WriteEndDocument();
            _xmlWriter.Flush();
            _xmlWriter.Close();
            return _sb.ToString();
        }

        public void WriteNameAndValue(string name, Value v)
        {
            _xmlWriter.WriteElementString(name, v.ToString());
        }

        public void StartNamedElement(string name)
        {
            _xmlWriter.WriteStartElement(name);
        }

        public void EndElement()
        {
            _xmlWriter.WriteEndElement();
        }

        public void StartNamedArray(string name)
        {
            throw new XmlException("Arrays are not supported in xml writer");
        }

        public void EndArray()
        {
            throw new XmlException("Arrays are not supported in xml writer");
        }

        public void StartElement()
        {
            if (!_hasStarted)
                StartNamedElement("root");
            else
                _xmlWriter.WriteStartElement("");
        }

        public void StartArray()
        {
            throw new XmlException("Arrays are not supported in xml writer");
        }

        public void WriteValue(Value v)
        {
            throw new XmlException("unnamed values in xml writer");
        }
    }
}
