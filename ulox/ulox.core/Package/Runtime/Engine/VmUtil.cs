using System.Linq;
using static ULox.Vm;

namespace ULox
{
    public static class VmUtil
    {
        public static string GenerateGlobalsDump(Vm vm)
        {
            var sb = new System.Text.StringBuilder();

            foreach (var item in vm.Globals)
            {
                sb.Append($"{item.Key} : {item.Value}");
            }

            return sb.ToString();
        }
        
        public static string GenerateCallStackDump(Vm vm)
        {
            var sb = new System.Text.StringBuilder();

            for (int i = 0; i < vm.CallFrames.Count; i++)
            {
                var cf = vm.CallFrames.Peek(i);
                sb.AppendLine(GetLocationNameFromFrame(cf));
            }

            return sb.ToString();
        }
        
        public static string GenerateValueStackDump(Vm vm) => DumpStack(vm.ValueStack);

        public static string GenerateReturnDump(Vm vm) => DumpStack(vm.ReturnStack);
        
        private static string DumpStack(FastStack<Value> valueStack)
        {
            var stackVars = valueStack
                .Select(x => x.ToString())
                .Take(valueStack.Count)
                .Reverse();

            return string.Join(System.Environment.NewLine, stackVars);
        }

        internal static string GetLocationNameFromFrame(CallFrame frame, int currentInstruction = -1)
        {
            if (frame.nativeFunc != null)
            {
                var name = frame.nativeFunc.Method.Name;
                if (frame.nativeFunc.Target != null)
                    name = frame.nativeFunc.Target.GetType().Name + "." + frame.nativeFunc.Method.Name;
                return $"native:'{name}'";
            }

            var line = -1;
            if (currentInstruction != -1)
                line = frame.Closure?.chunk?.GetLineForInstruction(currentInstruction) ?? -1;

            var locationName = frame.Closure?.chunk.GetLocationString(line);
            return $"chunk:'{locationName}'";
        }
    }
}
