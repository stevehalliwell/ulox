using System.Linq;
using System.Text;

namespace ULox
{
    public class StringBuilderValueHierarchyWriter : IValueHierarchyWriter
    {
        private readonly StringBuilder _sb = new();
        private int _indent = -1;

        public string GetString() => _sb.ToString();

        public void EndElement() => _indent--;

        private void AppendIndent()
        {
            if(_indent > 0)
                _sb.Append(string.Concat(Enumerable.Repeat("  ", _indent)));
        }

        public void WriteNameAndValue(string name, Value v)
        {
            AppendIndent();
            _sb.AppendLine($"{name}:{v}");
        }

        public void StartNamedElement(string name)
        {
            AppendIndent();
            _sb.AppendLine(name);
            _indent++;
        }

        public void StartArray(string name)
        {
            AppendIndent();
            _sb.AppendLine($"{name}:[");
        }

        public void EndArray()
        {
            AppendIndent();
            _sb.AppendLine($"]");
        }

        public void StartElement()
        {
            _indent++;
        }

        public void WriteValue(Value v)
        {
            AppendIndent();
            _sb.AppendLine($"{v}");
        }
    }
}
