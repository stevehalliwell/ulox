using Newtonsoft.Json;
using System;
using System.IO;

namespace ULox
{
    public class JsonDocValueHierarchyTraverser : DocValueHierarchyTraverser
    {
        private readonly JsonTextReader _reader;

        public JsonDocValueHierarchyTraverser(
            ValueObjectBuilder valBuilder,
            TextReader reader)
            : base(valBuilder)
        {
            _reader = new JsonTextReader(reader);
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
                case JsonToken.Integer:
                    Field(prevPropName, (long)_reader.Value);
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
