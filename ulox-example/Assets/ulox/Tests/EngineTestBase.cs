using NUnit.Framework;

namespace ULox.Tests
{
    public class EngineTestBase
    {
        protected ByteCodeInterpreterTestEngine testEngine;

        [SetUp]
        public void Setup()
        {
            testEngine = new ByteCodeInterpreterTestEngine(UnityEngine.Debug.Log);
        }
    }
}