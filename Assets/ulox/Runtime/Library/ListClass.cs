using System.Collections.Generic;

namespace ULox
{
    public class ListClass : ClassInternal
    {
        private const string ListFieldName = "list";

        private class InternalList : List<Value> { }

        public ListClass()
        {
            this.name = "List";
            this.AddMethod(ClassCompilette.InitMethodName, Value.New(InitInstance));
            this.AddMethod(nameof(Count), Value.New(Count));
            this.AddMethod(nameof(Resize), Value.New(Resize));
            this.AddMethod(nameof(Get), Value.New(Get));
            this.AddMethod(nameof(Set), Value.New(Set));
            this.AddMethod(nameof(Add), Value.New(Add));
            this.AddMethod(nameof(Remove), Value.New(Remove));
            this.AddMethod(nameof(Empty), Value.New(Empty));
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
