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

        private Value InitInstance(VMBase vm, int argCount)
        {
            var inst = vm.GetArg(0);
            inst.val.asInstance.SetField(ListFieldName, Value.Object(new InternalList()));
            return inst;
        }

        private Value Count(VMBase vm, int argCount)
        {
            InternalList list = GetArg0InternalList(vm);
            return Value.New(list.Count);
        }

        private Value Resize(VMBase vm, int argCount)
        {
            var inst = vm.GetArg(0);
            InternalList list = GetArg0InternalList(vm);
            int size = (int)vm.GetArg(1).val.asDouble;
            while (list.Count < size)
                list.Add(Value.Null());

            return inst;
        }

        private Value Get(VMBase vm, int argCount)
        {
            InternalList list = GetArg0InternalList(vm);
            int index = (int)vm.GetArg(1).val.asDouble;
            return list[index];
        }

        private Value Set(VMBase vm, int argCount)
        {
            InternalList list = GetArg0InternalList(vm);
            int index = (int)vm.GetArg(1).val.asDouble;
            var newValue = vm.GetArg(2);
            list[index] = newValue;
            return newValue;
        }

        private Value Add(VMBase vm, int argCount)
        {
            InternalList list = GetArg0InternalList(vm);
            var newValue = vm.GetArg(1);
            list.Add(newValue);
            return newValue;
        }

        private Value Remove(VMBase vm, int argCount)
        {
            InternalList list = GetArg0InternalList(vm);
            var toRemove = vm.GetArg(1);
            return Value.New(list.Remove(toRemove));
        }

        private Value Empty(VMBase vm, int argCount)
        {
            InternalList list = GetArg0InternalList(vm);
            return Value.New(list.Count == 0);
        }

        private static InternalList GetArg0InternalList(VMBase vm)
        {
            var inst = vm.GetArg(0);
            var list = inst.val.asInstance.GetField(ListFieldName).val.asObject as InternalList;
            return list;
        }
    }
}
