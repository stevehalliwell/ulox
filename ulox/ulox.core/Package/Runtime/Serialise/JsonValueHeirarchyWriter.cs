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
            _jsonWriter.Formatting = Formatting.Indented;
        }

        public string GetString()
            => _sb.ToString();

        public void WriteNameAndValue(string name, Value v)
        {
            _jsonWriter.WritePropertyName(name);
            _jsonWriter.WriteValue(v.ToString());
        }

        public void StartNamedElement(string name)
        {
            if(_hasStarted)
                _jsonWriter.WritePropertyName(name);
            _jsonWriter.WriteStartObject();
            _hasStarted = true;
        }

        public void EndElement()
            => _jsonWriter.WriteEndObject();

        public void StartNamedArray(string name)
        {
            _jsonWriter.WritePropertyName(name);
            _jsonWriter.WriteStartArray();
        }

        public void EndArray()
            => _jsonWriter.WriteEndArray();

        public void StartElement()
            => _jsonWriter.WriteStartObject();

        public void StartArray() 
            => _jsonWriter.WriteStartArray();

        public void WriteValue(Value v) 
            => _jsonWriter.WriteValue(v.ToString());
    }
}
