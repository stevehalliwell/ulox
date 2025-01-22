using Newtonsoft.Json;
using System.IO;
using System.Text;

namespace ULox
{
    public class JsonValueHierarchyWriter : IValueHierarchyWriter
    {
        private readonly JsonTextWriter _jsonWriter;
        private readonly StringBuilder _sb;

        public JsonValueHierarchyWriter()
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
            WriteValue(v);
        }

        public void StartNamedElement(string name)
        {
            _jsonWriter.WritePropertyName(name);
            _jsonWriter.WriteStartObject();
        }

        public void EndElement()
            => _jsonWriter.WriteEndObject();

        public void StartArray(string name)
        {
            _jsonWriter.WritePropertyName(name);
            _jsonWriter.WriteStartArray();
        }

        public void EndArray()
            => _jsonWriter.WriteEndArray();

        public void StartElement()
            => _jsonWriter.WriteStartObject();

        public void WriteValue(Value v)
        {
            if (v.type == ValueType.String)
                _jsonWriter.WriteValue(v.val.asString.String);
            else if (v.type == ValueType.Double)
                _jsonWriter.WriteValue(v.val.asDouble);
            else if (v.type == ValueType.Bool)
                _jsonWriter.WriteValue(v.val.asBool);
        }
    }
}
