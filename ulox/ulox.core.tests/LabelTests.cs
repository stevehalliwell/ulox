﻿using NUnit.Framework;

namespace ULox.Core.Tests
{
    public class LabelTests : EngineTestBase
    {
        [Test]
        public void Engine_MapEmtpy_Count0()
        {
            testEngine.Run(@"
goto end;
print(fail);
label end;
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }
    }
}
