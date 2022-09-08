using System;

namespace ULox
{
    public class RuntimeUloxException : UloxException
    {
        public RuntimeUloxException(string msg, int currentInstruction, string locationName, int line, string valueStack, string callStack)
            : base($"{msg} at ip:'{currentInstruction}' in chunk:'{locationName}':{line}.{Environment.NewLine}" +
                  $"===Stack==={Environment.NewLine}{valueStack}{Environment.NewLine}" +
                  $"===CallStack==={Environment.NewLine}{callStack}{Environment.NewLine}")
        {
        }
    }
}
