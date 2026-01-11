namespace ULox
{
    internal static class BuildStdLibrary
    {
        internal static InstanceInternal MakeInstance()
        {
            var buildInst = new InstanceInternal();
            buildInst.AddFieldsToInstance(
                (nameof(QueueFile), Value.New(QueueFile, 1, 1)),
                (nameof(ProcessQueue), Value.New(ProcessQueue, 1, 0)),
                (nameof(Interpret), Value.New(Interpret, 1, 1)),
                (nameof(ReinterpretOnEachCompile), Value.New(ReinterpretOnEachCompile, 1, 1))
                );
            buildInst.Freeze();
            return buildInst;
        }

        private static NativeCallResult QueueFile(Vm vm)
        {
            var toCompile = vm.GetArg(1);
            vm.Engine.LocateAndQueue(toCompile.val.asString.String);
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult ProcessQueue(Vm vm)
        {
            vm.Engine.BuildAndRun();
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult Interpret(Vm vm)
        {
            var toInterpret = vm.GetArg(1);
            var script = new Script("<Interpret>", toInterpret.val.asString.String);
            vm.Engine.RunScript(script);
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult ReinterpretOnEachCompile(Vm vm)
        {
            var toSet = vm.GetArg(1);
            vm.SetNativeReturn(0, Value.New(vm.Engine.Context.ReinterpretOnEachCompile));
            vm.Engine.Context.ReinterpretOnEachCompile = !toSet.IsFalsey();
            return NativeCallResult.SuccessfulExpression;
        }
    }
}
