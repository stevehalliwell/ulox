namespace ULox
{
    internal static class PlatformStdLibrary
    {
        internal static InstanceInternal MakeInstance()
        {
            var platformLibInst = new InstanceInternal();
            platformLibInst.AddFieldsToInstance(
                (nameof(FindFiles), Value.New(FindFiles, 1, 3)),
                (nameof(ReadFile), Value.New(ReadFile, 1, 1)),
                (nameof(WriteFile), Value.New(WriteFile, 1, 2))
                );

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
            vm.SetNativeReturn(0, Value.New(arr));
            return NativeCallResult.SuccessfulExpression;
        }

        public static NativeCallResult ReadFile(Vm vm)
        {
            var platform = vm.Engine.Context.Platform;
            var path = vm.GetArg(1).val.asString.String;
            var res = platform.LoadFile(path);
            vm.SetNativeReturn(0, Value.New(res));
            return NativeCallResult.SuccessfulExpression;
        }

        public static NativeCallResult WriteFile(Vm vm)
        {
            var platform = vm.Engine.Context.Platform;
            var path = vm.GetArg(1).val.asString.String;
            var data = vm.GetArg(2).val.asString.String;
            platform.SaveFile(path, data);
            return NativeCallResult.SuccessfulExpression;
        }
    }
}
