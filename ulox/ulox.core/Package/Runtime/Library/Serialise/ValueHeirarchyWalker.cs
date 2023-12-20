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
            WalkField(null, v);
        }

        private void WalkField(HashedString name, Value v)
        {
            switch (v.type)
            {
            case ValueType.Instance:
                if (v.val.asInstance is NativeListInstance listInst)
                {
                    _writer.StartArray(name.String);

                    foreach (var elm in listInst.List)
                    {
                        WalkField(null, elm);
                    }
                    _writer.EndArray();
                }
                else if (v.val.asInstance.Fields.Count == 0)
                {
                    WalkField(name, Value.Null());
                }
                else
                {
                    if (null == name)
                        _writer.StartElement();
                    else
                        _writer.StartNamedElement(name.String);

                    var fields = v.val.asInstance.Fields;
                    foreach (var field in fields.OrderBy(x => x.Key.String))
                    {
                        WalkField(field.Key, field.Value);
                    }
                    _writer.EndElement();
                }

                break;

            default:
                if (name != null)
                    _writer.WriteNameAndValue(name.String, v);
                else
                    _writer.WriteValue(v);

                break;
            }
        }
    }
}
