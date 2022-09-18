﻿using System;

namespace ULox
{
    public class RuntimeUloxException : UloxException
    {
        public RuntimeUloxException(string msg, int currentInstruction, string locationName, string valueStack, string callStack)
            : base($"{msg} at ip:'{currentInstruction}' in {locationName}.{Environment.NewLine}" +
                  $"===Stack==={Environment.NewLine}{valueStack}{Environment.NewLine}" +
                  $"===CallStack==={Environment.NewLine}{callStack}{Environment.NewLine}")
        {
        }
    }
}
