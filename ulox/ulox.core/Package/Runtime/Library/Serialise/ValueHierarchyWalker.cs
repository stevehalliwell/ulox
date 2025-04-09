using System.Linq;

namespace ULox
{
    public interface IValueHierarchyWriter
    {
        void StartNamedElement(string name);
        void StartElement();
        void EndElement();
        void StartArray(string name);
        void EndArray();
        void WriteNameAndValue(string name, Value v);
        void WriteValue(Value v);
    }
    
    public class ValueHierarchyWalker
    {
        private readonly IValueHierarchyWriter _writer;

        public ValueHierarchyWalker(IValueHierarchyWriter writer)
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
                    _writer.StartArray(name?.String ?? string.Empty);

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

            case ValueType.Double:
            case ValueType.Bool:
            case ValueType.String:
            case ValueType.Null:
            case ValueType.Object:
                if (name != null)
                    _writer.WriteNameAndValue(name.String, v);
                else
                    _writer.WriteValue(v);

                break;
            default:
                break;
            }
        }
    }
}
