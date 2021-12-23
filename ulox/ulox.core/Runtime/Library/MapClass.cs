﻿using System.Collections.Generic;

namespace ULox
{
    public class MapClass : ClassInternal
    {
        private static readonly HashedString MapFieldName = new HashedString("map");

        private class InternalMap : Dictionary<Value, Value>
        { }

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

        private NativeCallResult InitInstance(VMBase vm, int argCount)
        {
            var inst = vm.GetArg(0);
            inst.val.asInstance.SetField(MapFieldName, Value.Object(new InternalMap()));
            vm.PushReturn(inst);
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Count(VMBase vm, int argCount)
        {
            var inst = vm.GetArg(0);
            var map = inst.val.asInstance.GetField(MapFieldName).val.asObject as InternalMap;
            vm.PushReturn(Value.New(map.Count));
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Create(VMBase vm, int argCount)
        {
            var inst = vm.GetArg(0);
            var map = inst.val.asInstance.GetField(MapFieldName).val.asObject as InternalMap;
            var key = vm.GetArg(1);
            var val = vm.GetArg(2);

            if (map.ContainsKey(key))
            {
                vm.PushReturn(Value.New(false));
                return NativeCallResult.SuccessfulExpression;
            }

            map[key] = val;
            vm.PushReturn(Value.New(true));
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Read(VMBase vm, int argCount)
        {
            var inst = vm.GetArg(0);
            var map = inst.val.asInstance.GetField(MapFieldName).val.asObject as InternalMap;
            var key = vm.GetArg(1);

            if (map.TryGetValue(key, out var val))
            {
                vm.PushReturn(val);
                return NativeCallResult.SuccessfulExpression;
            }

            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Update(VMBase vm, int argCount)
        {
            var inst = vm.GetArg(0);
            var map = inst.val.asInstance.GetField(MapFieldName).val.asObject as InternalMap;
            var key = vm.GetArg(1);
            var val = vm.GetArg(2);

            if (!map.ContainsKey(key))
            {
                vm.PushReturn(Value.New(false));
                return NativeCallResult.SuccessfulExpression;
            }

            map[key] = val;
            vm.PushReturn(Value.New(true));
            return NativeCallResult.SuccessfulExpression;
        }

        private NativeCallResult Delete(VMBase vm, int argCount)
        {
            var inst = vm.GetArg(0);
            var map = inst.val.asInstance.GetField(MapFieldName).val.asObject as InternalMap;
            var key = vm.GetArg(1);

            vm.PushReturn(Value.New(map.Remove(key)));
            return NativeCallResult.SuccessfulExpression;
        }
    }
}
