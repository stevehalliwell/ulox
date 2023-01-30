using NUnit.Framework;

namespace ulox.core.tests
{
    [TestFixture]
    public class InnerVMTests : EngineTestBase
    {

        [Test]
        public void Engine_InternalSandbox_CanPassIn()
        {
            testEngine.Run(@"
fun InnerMain()
{
    print(globalIn);
}

var globalIn = 10;

var innerVM = VM();
innerVM.AddGlobal(""globalIn"",globalIn);
innerVM.AddGlobal(""print"",print);

innerVM.Start(InnerMain);"
            );

            Assert.AreEqual("10", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_InternalSandbox_CanWriteGlobalOut()
        {
            testEngine.Run(@"
fun InnerMain()
{
    globalOut = 10;
}

var globalOut = 0;

var innerVM = VM();
innerVM.AddGlobal(""globalOut"",globalOut);

innerVM.Start(InnerMain);

print(innerVM.GetGlobal(""globalOut""));");

            Assert.AreEqual("10", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_InternalSandbox_CanReturnOut()
        {
            testEngine.Run(@"
fun InnerMain()
{
    retval = 10;
}

var innerVM = VM();

var res = innerVM.Start(InnerMain);

print(res);");

            Assert.AreEqual("10", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_InternalSandbox_YieldResume()
        {
            testEngine.Run(@"
var a = 2;
a = a + 2;
yield;
print(a);"
            );

            testEngine.MyEngine.Context.Vm.Run();

            Assert.AreEqual("4", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_ChildVM_Run()
        {
            testEngine.Run(@"
fun InnerMain()
{
    print(""Hello from inner "" + a);
}

var a = ""10"";

var innerVM = VM();
innerVM.InheritFromEnclosing();
innerVM.Start(InnerMain);");

            Assert.AreEqual("Hello from inner 10", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Sandbox_CannotAccess()
        {
            testEngine.Run(@"
var a = 10;
fun InnerMain()
{
    a = 10;
}

var innerVM = VM();
innerVM.Start(InnerMain);"
            );
            StringAssert.StartsWith("Global var of name 'a' was not found at ip:'2' in chunk:'InnerMain(test:5)'.", testEngine.InterpreterResult, testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Sandbox_AsGenerator()
        {
            testEngine.Run(@"
fun InnerMain()
{
    globalOut = 1;
    yield;
    globalOut = 1;
    yield;
    globalOut = 2;
    yield;
    globalOut = 3;
    yield;
    globalOut = 5;
    yield;
    globalOut = 8;
    yield;
    globalOut = null;
}

var globalOut = 0;

var innerVM = VM();
innerVM.AddGlobal(""globalOut"",globalOut);

innerVM.Start(InnerMain);
loop
{
    var curVal = innerVM.GetGlobal(""globalOut"");
    if(curVal != null)
    {
        print(curVal);
        innerVM.Resume();
    }
    else
    {
        break;
    }
}");

            Assert.AreEqual("112358", testEngine.InterpreterResult);
        }
    }
}
