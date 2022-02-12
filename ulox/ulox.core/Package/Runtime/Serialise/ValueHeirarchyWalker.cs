using System.Linq;

namespace ULox
{
    public class ValueHeirarchyWalker
    {
        private readonly IValueHeirarchyWriter _writer;

        public ValueHeirarchyWalker(IValueHeirarchyWriter writer)
        {
            _writer = writer;
        }

        public void Walk(Value v)
        {
            WalkElement(new HashedString("root"), v);
        }

        private void WalkElement(HashedString name, Value v)
        {
            switch (v.type)
            {
            case ValueType.Instance:
                _writer.StartElement(name.String, v);
                foreach (var field in v.val.asInstance.Fields.OrderBy(x => x.Key.String))
                {
                    WalkElement(field.Key, field.Value);
                }
                _writer.EndElement(name.String, v);
                break;

            default:
                _writer.WriteNameAndValue(name.String, v);
                break;
            }
        }
    }
}
