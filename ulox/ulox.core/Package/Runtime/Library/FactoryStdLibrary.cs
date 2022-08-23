﻿namespace ULox
{
    internal static class FactoryStdLibrary
    {
        internal static InstanceInternal MakeFactoryInstance()
        {
            var factoryInst = new InstanceInternal();
            factoryInst.AddFieldsToInstance(
                (nameof(Line), Value.New(Line)),
                (nameof(SetLine), Value.New(SetLine))
                );
            factoryInst.Freeze();
            return factoryInst;
        }

        private static NativeCallResult Line(Vm vm, int argCount)
        {
            var arg = vm.GetArg(1);
            var line = vm.Factory.GetLine(arg);
            vm.PushReturn(line);
            return NativeCallResult.SuccessfulExpression;
        }

        private static NativeCallResult SetLine(Vm vm, int argCount)
        {
            var key = vm.GetArg(1);
            if (key.IsNull)
                throw new VMException($"'{nameof(SetLine)}' must have non null key argument.");
            var line = vm.GetArg(2);
            if (line.IsNull)
                throw new VMException($"'{nameof(SetLine)}' must have non null line argument.");

            vm.Factory.SetLine(key, line);
            return NativeCallResult.SuccessfulExpression;
        }
    }
}