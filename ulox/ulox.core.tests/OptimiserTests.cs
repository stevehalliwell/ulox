using System.Linq;
using NUnit.Framework;

namespace ULox.Core.Tests
{
    [TestFixture]
    public class OptimiserTests : EngineTestBase
    {
        private ByteCodeOptimiser _opt;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            _opt = testEngine.MyEngine.Context.Program.Optimiser;
            _opt.Enabled = true;
            _opt.EnableRemoveUnreachableLabels = true;
            _opt.EnableLocalizing = true;
            _opt.OptimisationReporter = new OptimisationReporter();
        }
        
        [Test]
        public void Optimiser_NothingToOptimise_DoesNothing()
        {
            testEngine.Run("print (1+2);");

            Assert.AreEqual("3", testEngine.InterpreterResult);
            Assert.AreEqual(7, testEngine.MyEngine.Context.Program.CompiledScripts[0].TopLevelChunk.Instructions.Count);
        }

        [Test]
        public void Optimiser_UnusedLabel_RemovesDeadCode()
        {
            testEngine.Run(@"
label unused;
print (1+2);");

            Assert.AreEqual("3", testEngine.InterpreterResult);
            StringAssert.Contains("Instructions: 8 -> 7", _opt.OptimisationReporter.GetReport());
        }

        [Test]
        public void Optimiser_UnusedLabelNotAtStart_RemovesDeadCode()
        {
            testEngine.Run(@"
print (1+2);
label unused;
print (1+2);");

            Assert.AreEqual("33", testEngine.InterpreterResult);
            StringAssert.Contains("Instructions: 14 -> 13", _opt.OptimisationReporter.GetReport());
        }

        [Test]
        public void Optimiser_JumpOps_RemovesUnreachable()
        {
            testEngine.Run(@"
goto skip;
print(1);
goto skip;
label skip;
print(1+2);");

            Assert.AreEqual("3", testEngine.InterpreterResult);
            StringAssert.Contains("Instructions: 14 -> 7", _opt.OptimisationReporter.GetReport());
        }

        [Test]
        public void Optimiser_GotoLabelWithDeadInBetween_RemovesGotoAndLabel()
        {
            testEngine.Run(@"
goto skip;
print(1);
label skip;
print(1+2);");

            Assert.AreEqual("3", testEngine.InterpreterResult);
            StringAssert.Contains("Instructions: 13 -> 7", _opt.OptimisationReporter.GetReport());
        }

        [Test]
        public void Optimiser_JumpNowhere_RemovesUselessJump()
        {
            testEngine.Run(@"
goto skip;
label skip;
print(1+2);");

            Assert.AreEqual("3", testEngine.InterpreterResult);
            StringAssert.Contains("Instructions: 9 -> 7", _opt.OptimisationReporter.GetReport());
        }
        
        [Test]
        public void Optimiser_RegisterableMath_CollapsesToRegisterBasedOp()
        {
            testEngine.Run(@"
{
var a = 1;
var b = 2;
var c = 0;
c = a + b;
print (c);
}");

            Assert.AreEqual("3", testEngine.InterpreterResult);
            StringAssert.Contains("Instructions: 14 -> 12", _opt.OptimisationReporter.GetReport());
        }

        [Test]
        public void Optimiser_RegisterableCompare_CollapsesToRegisterBasedOp()
        {
            testEngine.Run(@"
{
var a = 1;
var b = 2;
var c = true;
c = a == b;
print (c);
}");

            Assert.AreEqual("False", testEngine.InterpreterResult);
            StringAssert.Contains("Instructions: 14 -> 12", _opt.OptimisationReporter.GetReport());
        }

        [Test]
        public void Optimiser_ChainCalls_CollapsesToRegisterBasedOp()
        {
            testEngine.Run(@"
{
var a = 1;
var b = 2;
var c = 3;
var d = 0;
d = a+b-c;
print (d);
}");

            Assert.AreEqual("0", testEngine.InterpreterResult);
            StringAssert.Contains("Instructions: 17 -> 14", _opt.OptimisationReporter.GetReport());
        }

        [Test]
        public void Optimiser_GetIndex_CollapsesToRegisterBasedOp()
        {
            testEngine.Run(@"
var l = [1,2,3,4];
{
var a = 3;
var res = l[a];
print (res);
}");

            Assert.AreEqual("4", testEngine.InterpreterResult);
            StringAssert.Contains("Instructions: 20 -> 19", _opt.OptimisationReporter.GetReport());
        }

        [Test]
        public void Optimiser_SetIndex_CollapsesToRegisterBasedOp()
        {
            testEngine.Run(@"
var l = [1,2,3,4];
{
var index = 3;
var newVal = 0; 
l[index] = newVal;
print (l[index]);
}");

            Assert.AreEqual("0", testEngine.InterpreterResult);
            StringAssert.Contains("Instructions: 25 -> 22", _opt.OptimisationReporter.GetReport());
        }

        [Test]
        public void Optimiser_Negate_CollapsesToRegisterBasedOp()
        {
            testEngine.Run(@"
{
var a = 1;
a = -a;
print(a);
}");

            Assert.AreEqual("-1", testEngine.InterpreterResult);
            StringAssert.Contains("Instructions: 11 -> 10", _opt.OptimisationReporter.GetReport());
        }

        [Test]
        public void Optimiser_Not_CollapsesToRegisterBasedOp()
        {
            testEngine.Run(@"
{
var a = 1;
a = ! a;
print(a);
}");

            Assert.AreEqual("False", testEngine.InterpreterResult);
            StringAssert.Contains("Instructions: 11 -> 10", _opt.OptimisationReporter.GetReport());
        }

        [Test]
        public void Optimiser_CountOf_CollapsesToRegisterBasedOp()
        {
            testEngine.Run(@"
{
var l = [];
var a = 1;
a = countof l;
print(a);
}");

            Assert.AreEqual("0", testEngine.InterpreterResult);
            StringAssert.Contains("Instructions: 13 -> 12", _opt.OptimisationReporter.GetReport());
        }

        [Test]
        public void Optimiser_SetProp_CollapsesToRegisterBasedOp()
        {
            testEngine.Run(@"
{
var obj = {a=1};
var newVal = 2;
obj.a = newVal;
print(obj.a);
}");

            Assert.AreEqual("2", testEngine.InterpreterResult);
            StringAssert.Contains("Instructions: 17 -> 14", _opt.OptimisationReporter.GetReport());
        }

        [Test]
        public void Optimiser_GetProp_CollapsesToRegisterBasedOp()
        {
            testEngine.Run(@"
{
var obj = {a=1};
var val = 2;
val = obj.a;
print(val);
}");

            Assert.AreEqual("1", testEngine.InterpreterResult);
            StringAssert.Contains("Instructions: 16 -> 15", _opt.OptimisationReporter.GetReport());
        }

        [Test]
        public void Optimiser_GetSetPropInClass_CollapsesToRegisterBasedOp()
        {
            testEngine.Run(@"
class Foo
{
    var a = 1;
    Meth() {this.a = this.a + this.a;}
}

var f = Foo();
f.Meth();

print(f.a);
");

            Assert.AreEqual("2", testEngine.InterpreterResult);
            StringAssert.Contains("Instructions: 9 -> 7", _opt.OptimisationReporter.GetReport());
        }
    }
}
