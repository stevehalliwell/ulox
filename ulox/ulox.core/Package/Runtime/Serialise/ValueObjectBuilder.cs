using System;
using System.Collections.Generic;

namespace ULox
{
    public class ValueObjectBuilder : IValueObjectBuilder
    {
        public enum ObjectType { Object, Array }
        private readonly ObjectType _objectType;

        private Value _value;
        private List<(string name, ValueObjectBuilder builder)> _children = new List<(string, ValueObjectBuilder)>();

        public ValueObjectBuilder(ObjectType ot)
        {
            _objectType = ot;

            switch (_objectType)
            {
            case ObjectType.Object:
                _value = Value.New(new InstanceInternal());
                break;
            case ObjectType.Array:
                _value = Value.New(NativeListClass.CreateInstance());
                break;
            default:
                break;
            }
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

        public void SetField(string name, Value data)
        {
            switch (_objectType)
            {
            case ObjectType.Object:
                _value.val.asInstance.SetField(new HashedString(name), data);
                break;
            case ObjectType.Array:
                (_value.val.asInstance as NativeListInstance).List.Add(data);
                break;
            default:
                throw new Exception();
            }
        }

        public IValueObjectBuilder CreateChild(string name)
        {
            var child = new ValueObjectBuilder(ObjectType.Object);
            _children.Add((name, child));
            return child;
        }

        public IValueObjectBuilder CreateArray(string name)
        {
            var child = new ValueObjectBuilder(ObjectType.Array);
            _children.Add((name, child));
            return child;
        }
    }
}
