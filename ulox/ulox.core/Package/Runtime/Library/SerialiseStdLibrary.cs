using System.IO;

namespace ULox
{
    internal static class SerialiseStdLibrary
    {
        internal static InstanceInternal MakeInstance()
        {
            var serialiseInst = new InstanceInternal();
            serialiseInst.AddFieldsToInstance(
                (nameof(ToJson), Value.New(ToJson, 1, 1)),
                (nameof(FromJson), Value.New(FromJson, 1, 1)));
            serialiseInst.Freeze();
            return serialiseInst;
        }

        private static NativeCallResult ToJson(Vm vm)
        {
            var obj = vm.GetArg(1);

            if (obj.type != ValueType.Instance
                || obj.val.asInstance is NativeListInstance)
            {
                vm.SetNativeReturn(0, Value.New(string.Empty));
                return NativeCallResult.SuccessfulExpression;
            }

            var jsonWriter = new JsonValueHierarchyWriter();
            var walker = new ValueHierarchyWalker(jsonWriter);
            walker.Walk(obj);
            var result = jsonWriter.GetString();
            vm.SetNativeReturn(0, Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult FromJson(Vm vm)
        {
            var retval = Value.Null();
            try
            {
                var jsonString = vm.GetArg(1);
                var reader = new StringReader(jsonString.val.asString.String);
                var creator = new JsonDocValueHierarchyTraverser(new ValueObjectBuilder(ValueObjectBuilder.ObjectType.Object), reader);
                creator.Process();
                retval = creator.Finish();
            }
            catch (System.Exception)
            {
            }
            vm.SetNativeReturn(0, retval);
            return NativeCallResult.SuccessfulExpression;
        }
    }
}
