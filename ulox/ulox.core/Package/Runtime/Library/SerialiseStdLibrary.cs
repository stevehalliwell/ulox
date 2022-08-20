using System.IO;

namespace ULox
{
    internal static class SerialiseStdLibrary
    {
        internal static InstanceInternal MakeSerialiseInstance()
        {
            var serialiseInst = new InstanceInternal();
            serialiseInst.AddFieldsToInstance(
                (nameof(ToJson), Value.New(ToJson)),
                (nameof(FromJson), Value.New(FromJson)));
            serialiseInst.Freeze();
            return serialiseInst;
        }

        private static NativeCallResult ToJson(Vm vm, int argCount)
        {
            var obj = vm.GetArg(1);
            var jsonWriter = new JsonValueHeirarchyWriter();
            var walker = new ValueHeirarchyWalker(jsonWriter);
            walker.Walk(obj);
            var result = jsonWriter.GetString();
            vm.PushReturn(Value.New(result));
            return NativeCallResult.SuccessfulExpression;
        }
        
        private static NativeCallResult FromJson(Vm vm, int argCount)
        {
            var jsonString = vm.GetArg(1);
            var reader = new StringReader(jsonString.val.asString.String);
            var creator = new JsonDocValueHeirarchyTraverser(new ValueObjectBuilder(ValueObjectBuilder.ObjectType.Object), reader);
            creator.Process();
            var obj = creator.Finish();
            vm.PushReturn(obj);
            return NativeCallResult.SuccessfulExpression;
        }
    }
}
