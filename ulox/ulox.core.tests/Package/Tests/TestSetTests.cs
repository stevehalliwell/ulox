using NUnit.Framework;

namespace ULox.Tests
{
    public class TestSetTests : EngineTestBase
    {
        [Test]
        public void Engine_TestCase_Empty()
        {
            testEngine.Run(@"
test T
{
    testcase A
    {
    }
}"
            );

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_TestCase_Simple1()
        {
            testEngine.Run(@"
test T
{
    testcase A
    {
        print(2);
    }
}");

            Assert.AreEqual("2", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_TestCase_Simple2()
        {
            testEngine.Run(@"
test T
{
    testcase A
    {
        Assert.AreEqual(2,2);
    }
}");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_TestCase_Simple3()
        {
            testEngine.Run(@"
test T
{
    testcase A
    {
        var a = 2;
        var b = 3;
        Assert.AreNotEqual(a,b);
    }
}");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_TestCase_Simple4()
        {
            testEngine.Run(@"
test T
{
    testcase A
    {
        var a = 2;
        var b = 3;
        var c = a + b;
        Assert.AreEqual(c,5);
    }
}"
            );

            Assert.AreEqual("", testEngine.InterpreterResult);
            Assert.AreEqual("T:A Completed", testEngine.MyEngine.Context.VM.TestRunner.GenerateDump());
        }

        [Test]
        public void Engine_TestCase_MultipleEmpty()
        {
            testEngine.Run(@"
test T
{
    testcase A
    {
    }
    testcase B
    {
    }
}");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_TestCase_ReportAll()
        {
            testEngine.Run(@"
test T
{
    testcase A
    {
        throw;
    }
    testcase B
    {
    }
    testcase C
    {
        throw;
    }
}"
            );

            Assert.AreEqual("", testEngine.InterpreterResult);
            var completeReport = testEngine.MyEngine.Context.VM.TestRunner.GenerateDump();
            StringAssert.Contains("T:A Incomplete", completeReport);
            StringAssert.Contains("T:B Completed", completeReport);
            StringAssert.Contains("T:C Incomplete", completeReport);
        }

        [Test]
        public void Engine_TestCase_MultipleSimple()
        {
            testEngine.Run(@"
test T
{
    testcase A
    {
        var a = 1;
        var b = 2;
        var c = a + b;
        Assert.AreEqual(c,3);
    }
    testcase B
    {
        var a = 4;
        var b = 5;
        var c = a + b;
        Assert.AreEqual(c,9);
    }
}");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_TestCase_Simple4_Skipped()
        {
            testEngine.MyEngine.Context.VM.TestRunner.Enabled = false;

            testEngine.Run(@"
test T
{
    testcase A
    {
        var a = 2;
        var b = 3;
        var c = a + b;
        Assert.AreEqual(c,5);
    }
}"
            );

            Assert.AreEqual("", testEngine.InterpreterResult);
            Assert.AreEqual("", testEngine.MyEngine.Context.VM.TestRunner.GenerateDump());
        }


        //todo yield should be able to multi return, use a yield stack in the vm and clear it at each use?

        [Test]
        public void Engine_Test_ContextNames()
        {
            testEngine.Run(@"
test Foo
{
    testcase Bar
    {
        print(tsname);
        print(tcname);
    }
}");

            Assert.AreEqual("FooBar", testEngine.InterpreterResult);
        }
    }
}
