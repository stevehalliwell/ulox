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
            _opt.EnableLocalizing = true;
            _opt.OptimisationReporter = new OptimisationReporter();
        }
        
        [Test]
        public void Optimiser_NothingToOptimise_DoesNothing()
        {
            testEngine.Run("print (1+2);");

            Assert.AreEqual("3", testEngine.InterpreterResult);
            Assert.AreEqual(8, testEngine.MyEngine.Context.Program.CompiledScripts[0].TopLevelChunk.Instructions.Count);
        }

        [Test]
        public void Optimiser_UnusedLabel_RemovesDeadCode()
        {
            testEngine.Run(@"
label unused;
print (1+2);");

            Assert.AreEqual("3", testEngine.InterpreterResult);
            StringAssert.Contains("Instructions: 9 -> 8", _opt.OptimisationReporter.GetReport());
        }

        [Test]
        public void Optimiser_UnusedLabelNotAtStart_RemovesDeadCode()
        {
            testEngine.Run(@"
print (1+2);
label unused;
print (1+2);");

            Assert.AreEqual("33", testEngine.InterpreterResult);
            StringAssert.Contains("Instructions: 15 -> 14", _opt.OptimisationReporter.GetReport());
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
            StringAssert.Contains("Instructions: 15 -> 8", _opt.OptimisationReporter.GetReport());
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
            StringAssert.Contains("Instructions: 14 -> 8", _opt.OptimisationReporter.GetReport());
        }

        [Test]
        public void Optimiser_JumpNowhere_RemovesUselessJump()
        {
            testEngine.Run(@"
goto skip;
label skip;
print(1+2);");

            Assert.AreEqual("3", testEngine.InterpreterResult);
            StringAssert.Contains("Instructions: 10 -> 8", _opt.OptimisationReporter.GetReport());
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
            StringAssert.Contains("Instructions: 15 -> 13", _opt.OptimisationReporter.GetReport());
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
            StringAssert.Contains("Instructions: 15 -> 13", _opt.OptimisationReporter.GetReport());
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
            StringAssert.Contains("Instructions: 18 -> 15", _opt.OptimisationReporter.GetReport());
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
            StringAssert.Contains("Instructions: 21 -> 20", _opt.OptimisationReporter.GetReport());
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
            StringAssert.Contains("Instructions: 26 -> 23", _opt.OptimisationReporter.GetReport());
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
            StringAssert.Contains("Instructions: 12 -> 11", _opt.OptimisationReporter.GetReport());
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
            StringAssert.Contains("Instructions: 12 -> 11", _opt.OptimisationReporter.GetReport());
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
            StringAssert.Contains("Instructions: 14 -> 13", _opt.OptimisationReporter.GetReport());
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
            StringAssert.Contains("Instructions: 18 -> 15", _opt.OptimisationReporter.GetReport());
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
            StringAssert.Contains("Instructions: 17 -> 16", _opt.OptimisationReporter.GetReport());
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
            StringAssert.Contains("Instructions: 10 -> 8", _opt.OptimisationReporter.GetReport());
        }

        [Test]
        public void Optimiser_Loop_WhenBreakAt5_ShouldPrintUpTo6()
        {
            testEngine.Run(@"
var i = 0;
loop
{
    print (i);
    i = i + 1;
    if(i > 5)
        break;
    print (i);
}");

            Assert.AreEqual("01122334455", testEngine.InterpreterResult);
            StringAssert.Contains("Instructions: 40 -> 30", _opt.OptimisationReporter.GetReport());
        }


        [Test]
        public void Optimiser_InitWithManyArgsAndNoLocals_WhenCalled_ShouldSucceed()
        {
            testEngine.Run(@"
class T
{
    init(a,b,c,d,e,f,g)
    {
        print(d);
    }
}

var t = T(1,2,3,4,5,6,7);
");

            Assert.AreEqual("4", testEngine.InterpreterResult);
            StringAssert.Contains("Instructions: 6 -> 3", _opt.OptimisationReporter.GetReport());
        }

        [Test]
        public void Optimiser_Engine_Cycle_While_Nested_Locals()
        {
            testEngine.Run(@"
fun DoIt(){
var i = 0;
var j = 0;
while(i < 5)
{
    j= 0;
    while(j < 5)
    {
        j = j + 1;
        print (j);
        print (i);
    }
    i = i + 1;
    print (i);
}}

DoIt();");

            Assert.AreEqual("1020304050111213141512122232425231323334353414243444545", testEngine.InterpreterResult);
            StringAssert.Contains("GOTO: 10 -> 6", _opt.OptimisationReporter.GetReport());
        }


        [Test]
        public void Optimiser_Engine_Compile_Func_Inner_Logic()
        {
            testEngine.Run(@"
fun A(v)
{
    retval = 2;
    
    if(v > 5)
        return;
    retval = -1;
}
fun B(){retval = 3;}
fun C(){retval = 10;}

print( A(1)+B()*C());

print (A(10)+B()*C());");

            Assert.AreEqual("2932", testEngine.InterpreterResult);
            StringAssert.Contains("GOTO: 2 -> 1", _opt.OptimisationReporter.GetReport());
        }
    }
}
