using System.Linq;
using System.Text;

namespace ULox
{
    public class StringBuilderValueHeirarchyWriter : IValueHeirarchyWriter
    {
        private readonly StringBuilder _sb = new StringBuilder();
        private int _indent;

        public string GetString() => _sb.ToString();

        public void EndElement(string name, Value v) => _indent--;

        private void AppendIndent()
        {
            _sb.Append(string.Concat(Enumerable.Repeat("  ", _indent)));
        }

        public void WriteNameAndValue(string name, Value v)
        {
            AppendIndent();
            _sb.AppendLine($"{name}:{v}");
        }

        public void StartElement(string name, Value v)
        {
            AppendIndent();
            _sb.AppendLine(name);
            _indent++;
        }
    }
}
