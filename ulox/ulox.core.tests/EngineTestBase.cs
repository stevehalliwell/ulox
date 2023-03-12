using NUnit.Framework;

namespace ULox.Core.Tests
{
    public class EngineTestBase
    {
        protected ByteCodeInterpreterTestEngine testEngine;

        [SetUp]
        public virtual void Setup()
        {
            testEngine = new ByteCodeInterpreterTestEngine(System.Console.WriteLine);
        }
    }
}