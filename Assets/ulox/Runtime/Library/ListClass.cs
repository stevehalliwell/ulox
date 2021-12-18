using System.Collections.Generic;

namespace ULox
{
    public class ListClass : ClassInternal
    {
        private static readonly HashedString ListFieldName = new HashedString("list");

        private class InternalList : List<Value> { }

        public ListClass(): base(new HashedString("List"))
        {
            this.AddMethod(ClassCompilette.InitMethodName, Value.New(InitInstance));
            this.AddMethodsToClass(
                (nameof(Count), Value.New(Count)),
                (nameof(Resize), Value.New(Resize)),
                (nameof(Get), Value.New(Get)),
                (nameof(Set), Value.New(Set)),
                (nameof(Add), Value.New(Add)),
                (nameof(Remove), Value.New(Remove)),
                (nameof(Empty), Value.New(Empty))
                );
        }

        private NativeCallResult InitInstance(VMBase vm, int argCount)
        {
            var inst = vm.GetArg(0);
            inst.val.asInstance.SetField(ListFieldName, Value.Object(new InternalList()));
            vm.PushReturn(inst);
            return NativeCallResult.Success;
        }

        private NativeCallResult Count(VMBase vm, int argCount)
        {
            InternalList list = GetArg0InternalList(vm);
            vm.PushReturn(Value.New(list.Count));
            return NativeCallResult.Success;
        }

        private NativeCallResult Resize(VMBase vm, int argCount)
        {
            var inst = vm.GetArg(0);
            InternalList list = GetArg0InternalList(vm);
            int size = (int)vm.GetArg(1).val.asDouble;
            while (list.Count < size)
                list.Add(Value.Null());

            vm.PushReturn(inst);
            return NativeCallResult.Success;
        }

        private NativeCallResult Get(VMBase vm, int argCount)
        {
            InternalList list = GetArg0InternalList(vm);
            int index = (int)vm.GetArg(1).val.asDouble;
            vm.PushReturn(list[index]);
            return NativeCallResult.Success;
        }

        private NativeCallResult Set(VMBase vm, int argCount)
        {
            InternalList list = GetArg0InternalList(vm);
            int index = (int)vm.GetArg(1).val.asDouble;
            var newValue = vm.GetArg(2);
            list[index] = newValue;

            vm.PushReturn(newValue);
            return NativeCallResult.Success;
        }

        private NativeCallResult Add(VMBase vm, int argCount)
        {
            InternalList list = GetArg0InternalList(vm);
            var newValue = vm.GetArg(1);
            list.Add(newValue);
            vm.PushReturn(newValue);
            return NativeCallResult.Success;
        }

        private NativeCallResult Remove(VMBase vm, int argCount)
        {
            InternalList list = GetArg0InternalList(vm);
            var toRemove = vm.GetArg(1);
            vm.PushReturn(Value.New(list.Remove(toRemove)));
            return NativeCallResult.Success;
        }

        private NativeCallResult Empty(VMBase vm, int argCount)
        {
            InternalList list = GetArg0InternalList(vm);
            vm.PushReturn(Value.New(list.Count == 0));
            return NativeCallResult.Success;
        }

        private static InternalList GetArg0InternalList(VMBase vm)
        {
            var inst = vm.GetArg(0);
            var list = inst.val.asInstance.GetField(ListFieldName).val.asObject as InternalList;
            return list;
        }
    }
}
