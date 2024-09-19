using System;
using System.Collections.Generic;

namespace ULox
{
    public sealed class ValueObjectBuilder
    {
        public enum ObjectType { Object, Array }
        private readonly ObjectType _objectType;

        private Value _value;
        private readonly List<(string name, ValueObjectBuilder builder)> _children = new();

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
                throw new Exception();
            }
        }

        public Value Finish()
        {
            for (int i = _children.Count - 1; i >= 0; i--)
            {
                var (name, builder) = _children[i];
                var childVal = builder.Finish();
                _value.val.asInstance.SetField(new HashedString(name), childVal);
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

        public ValueObjectBuilder CreateChild(string name)
        {
            var child = new ValueObjectBuilder(ObjectType.Object);
            _children.Add((name, child));
            return child;
        }

        public ValueObjectBuilder CreateArray(string name)
        {
            var child = new ValueObjectBuilder(ObjectType.Array);
            _children.Add((name, child));
            return child;
        }
    }
}
