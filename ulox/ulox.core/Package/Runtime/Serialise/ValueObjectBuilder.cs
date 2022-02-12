using System.Collections.Generic;

namespace ULox
{
    public class ValueObjectBuilder : IValueObjectBuilder
    {
        private Value _value;
        private List<(string name, ValueObjectBuilder builder)> _children = new List<(string, ValueObjectBuilder)>();

        public ValueObjectBuilder()
        {
            _value = Value.New(new InstanceInternal());
        }

        public Value Finish()
        {
            for (int i = _children.Count - 1; i >= 0; i--)
            {
                var item = _children[i];
                var childVal = item.builder.Finish();
                _value.val.asInstance.SetField(new HashedString(item.name), childVal);
            }

            return _value;
        }

        public void SetField(string name, string data)
        {
            _value.val.asInstance.SetField(new HashedString(name), Value.New(data));
        }

        public IValueObjectBuilder CreateChild(string prevNodeName)
        {
            var child = new ValueObjectBuilder();
            _children.Add((prevNodeName, child));
            return child;
        }
    }
}
