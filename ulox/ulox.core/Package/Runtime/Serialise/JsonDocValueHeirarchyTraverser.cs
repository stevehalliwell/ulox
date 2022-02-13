using Newtonsoft.Json;
using System;

namespace ULox
{
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
                throw new Exception();
        }

        protected override void ProcessNode()
        {
            var prevPropName = string.Empty;
            while (_reader.Read())
            {
                Console.WriteLine($"{_reader.TokenType}_{_reader.Value}");
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
                case JsonToken.Float:
                    Field(prevPropName, (double)_reader.Value);
                    break;
                case JsonToken.Boolean:
                    Field(prevPropName, (bool)_reader.Value);
                    break;
                case JsonToken.EndObject:
                    EndChild();
                    return;
                case JsonToken.StartArray:
                    StartArray(prevPropName);
                    ProcessNode();
                    break;
                case JsonToken.EndArray:
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
