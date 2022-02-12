using System.Text;
using System.Xml;

namespace ULox
{
    public class XmlValueHeirarchyWriter : IValueHeirarchyWriter
    {
        private readonly XmlWriter _xmlWriter;
        private readonly StringBuilder _sb;

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

        public void StartElement(string name, Value v)
        {
            _xmlWriter.WriteStartElement(name);
        }

        public void EndElement(string name, Value v)
        {
            _xmlWriter.WriteEndElement();
        }
    }
}
