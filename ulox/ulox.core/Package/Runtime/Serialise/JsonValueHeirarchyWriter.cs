using Newtonsoft.Json;
using System.IO;
using System.Text;

namespace ULox
{
    public class JsonValueHeirarchyWriter : IValueHeirarchyWriter
    {
        private readonly JsonTextWriter _jsonWriter;
        private readonly StringBuilder _sb;
        private bool _hasStarted = false;

        public JsonValueHeirarchyWriter()
        {
            _sb = new StringBuilder();
            var sw = new StringWriter(_sb);
            _jsonWriter = new JsonTextWriter(sw);
            _jsonWriter.Formatting = Newtonsoft.Json.Formatting.Indented;
        }

        public string GetString()
        {
            return _sb.ToString();
        }

        public void WriteNameAndValue(string name, Value v)
        {
            _jsonWriter.WritePropertyName(name);
            _jsonWriter.WriteValue(v.ToString());
        }

        public void StartElement(string name, Value v)
        {
            if(_hasStarted)
                _jsonWriter.WritePropertyName(name);
            _jsonWriter.WriteStartObject();
            _hasStarted = true;
        }

        public void EndElement(string name, Value v)
        {
            _jsonWriter.WriteEndObject();
        }
    }
}
