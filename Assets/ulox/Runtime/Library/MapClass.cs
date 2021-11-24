using System.Collections.Generic;

namespace ULox
{
    public class MapClass : ClassInternal
    {
        private readonly static HashedString MapFieldName = new HashedString("map");

        private class InternalMap : Dictionary<Value, Value> { }

        public MapClass() : base(new HashedString("Map"))
        {
            this.AddMethod(ClassCompilette.InitMethodName, Value.New(InitInstance));
            this.AddMethodsToClass(
                (nameof(Count), Value.New(Count)),
                (nameof(Create), Value.New(Create)),
                (nameof(Read), Value.New(Read)),
                (nameof(Update), Value.New(Update)),
                (nameof(Delete), Value.New(Delete))
                );
        }

        private Value InitInstance(VMBase vm, int argCount)
        {
            var inst = vm.GetArg(0);
            inst.val.asInstance.SetField(MapFieldName, Value.Object(new InternalMap()));
            return inst;
        }

        private Value Count(VMBase vm, int argCount)
        {
            var inst = vm.GetArg(0);
            var map = inst.val.asInstance.GetField(MapFieldName).val.asObject as InternalMap;
            return Value.New(map.Count);
        }

        private Value Create(VMBase vm, int argCount)
        {
            var inst = vm.GetArg(0);
            var map = inst.val.asInstance.GetField(MapFieldName).val.asObject as InternalMap;
            var key = vm.GetArg(1);
            var val = vm.GetArg(2);

            if (map.ContainsKey(key))
            {
                return Value.New(false);
            }

            map[key] = val;
            return Value.New(true);
        }

        private Value Read(VMBase vm, int argCount)
        {
            var inst = vm.GetArg(0);
            var map = inst.val.asInstance.GetField(MapFieldName).val.asObject as InternalMap;
            var key = vm.GetArg(1);

            if (map.TryGetValue(key, out var val))
            {
                return val;
            }
            return Value.Null();
        }

        private Value Update(VMBase vm, int argCount)
        {
            var inst = vm.GetArg(0);
            var map = inst.val.asInstance.GetField(MapFieldName).val.asObject as InternalMap;
            var key = vm.GetArg(1);
            var val = vm.GetArg(2);

            if (!map.ContainsKey(key))
            {
                return Value.New(false);
            }

            map[key] = val;
            return Value.New(true);
        }

        private Value Delete(VMBase vm, int argCount)
        {
            var inst = vm.GetArg(0);
            var map = inst.val.asInstance.GetField(MapFieldName).val.asObject as InternalMap;
            var key = vm.GetArg(1);

            return Value.New(map.Remove(key));
        }
    }
}
