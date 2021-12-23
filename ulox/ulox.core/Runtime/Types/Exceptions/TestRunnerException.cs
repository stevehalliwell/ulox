using System;

namespace ULox
{
    [Serializable]
    public class TestRunnerException : Exception
    {
        public TestRunnerException()
        {
        }

        public TestRunnerException(string message) : base(message)
        {
        }

        public TestRunnerException(string message, Exception inner) : base(message, inner)
        {
        }

        protected TestRunnerException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
