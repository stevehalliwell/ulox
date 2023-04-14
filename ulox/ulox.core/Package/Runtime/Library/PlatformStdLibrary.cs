namespace ULox
{
    internal static class PlatformStdLibrary
    {
        internal static InstanceInternal MakeMathInstance()
        {
            var platformLibInst = new InstanceInternal();
            platformLibInst.AddFieldsToInstance(
                (nameof(FindFiles), Value.New(FindFiles, 1, 3)));

            platformLibInst.Freeze();
            return platformLibInst;
        }
        
        public static NativeCallResult FindFiles(Vm vm)
        {
            var platform = vm.Engine.Context.Platform;
            var path = vm.GetArg(1).val.asString.String;
            var pattern = vm.GetArg(2).val.asString.String;
            var recurse = vm.GetArg(3).val.asBool;
            var res = platform.FindFiles(path, pattern, recurse);
            var arr = NativeListClass.CreateInstance();
            foreach (var item in res)
            {
                arr.List.Add(Value.New(item));
            }
            vm.SetNativeReturn(0,Value.New(arr));
            return NativeCallResult.SuccessfulExpression;
        }
    }
}
