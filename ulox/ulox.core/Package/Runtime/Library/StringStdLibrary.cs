namespace ULox
{
    internal static class StringStdLibrary
    {
        internal static InstanceInternal MakeInstance()
        {
            var stringLibInst = new InstanceInternal();
            stringLibInst.AddFieldsToInstance(
                (nameof(Explode), Value.New(Explode, 1, 2))
                );
            stringLibInst.Freeze();
            return stringLibInst;
        }

        internal static NativeCallResult Explode(Vm vm)
        {
            var str = vm.GetArg(1).val.asString.String;
            var sep = vm.GetArg(2).val.asString.String;
            var res = str.Split(sep.ToCharArray(), System.StringSplitOptions.RemoveEmptyEntries);
            var arr = NativeListClass.CreateInstance();
            foreach (var item in res)
            {
                arr.List.Add(Value.New(item));
            }
            vm.SetNativeReturn(0, Value.New(arr));
            return NativeCallResult.SuccessfulExpression;
        }
    }
}
