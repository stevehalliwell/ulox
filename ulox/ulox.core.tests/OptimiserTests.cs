using NUnit.Framework;

namespace ULox.Core.Tests
{
    [TestFixture]
    public class OptimiserTests : EngineTestBase
    {
        private Optimiser _opt;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            _opt = testEngine.MyEngine.Context.Program.Optimiser;
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
            StringAssert.Contains("Instructions: 10 -> 7", _opt.OptimisationReporter.GetReport().GenerateStringReport());
        }

        [Test]
        public void Optimiser_UnusedLabelNotAtStart_RemovesDeadCode()
        {
            testEngine.Run(@"
print (1+2);
label unused;
print (1+2);");

            Assert.AreEqual("33", testEngine.InterpreterResult);
            StringAssert.Contains("Instructions: 16 -> 13", _opt.OptimisationReporter.GetReport().GenerateStringReport());
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
            StringAssert.Contains("Instructions: 16 -> 7", _opt.OptimisationReporter.GetReport().GenerateStringReport());
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
            StringAssert.Contains("Instructions: 15 -> 7", _opt.OptimisationReporter.GetReport().GenerateStringReport());
        }

        [Test]
        public void Optimiser_JumpNowhere_RemovesUselessJump()
        {
            testEngine.Run(@"
goto skip;
label skip;
print(1+2);");

            Assert.AreEqual("3", testEngine.InterpreterResult);
            StringAssert.Contains("Instructions: 11 -> 7", _opt.OptimisationReporter.GetReport().GenerateStringReport());
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
            StringAssert.Contains("Instructions: 15 -> 10", _opt.OptimisationReporter.GetReport().GenerateStringReport());
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
            StringAssert.Contains("Instructions: 15 -> 10", _opt.OptimisationReporter.GetReport().GenerateStringReport());
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
            StringAssert.Contains("Instructions: 18 -> 12", _opt.OptimisationReporter.GetReport().GenerateStringReport());
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
            StringAssert.Contains("Instructions: 21 -> 18", _opt.OptimisationReporter.GetReport().GenerateStringReport());
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
            StringAssert.Contains("Instructions: 26 -> 21", _opt.OptimisationReporter.GetReport().GenerateStringReport());
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
            StringAssert.Contains("Instructions: 12 -> 9", _opt.OptimisationReporter.GetReport().GenerateStringReport());
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
            StringAssert.Contains("Instructions: 12 -> 9", _opt.OptimisationReporter.GetReport().GenerateStringReport());
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
            StringAssert.Contains("Instructions: 13 -> 10", _opt.OptimisationReporter.GetReport().GenerateStringReport());
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
            StringAssert.Contains("Instructions: 18 -> 13", _opt.OptimisationReporter.GetReport().GenerateStringReport());
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
            StringAssert.Contains("Instructions: 17 -> 12", _opt.OptimisationReporter.GetReport().GenerateStringReport());
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
            StringAssert.Contains("Instructions: 10 -> 7", _opt.OptimisationReporter.GetReport().GenerateStringReport());
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
            StringAssert.Contains("Instructions: 40 -> 29", _opt.OptimisationReporter.GetReport().GenerateStringReport());
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
            StringAssert.Contains("Instructions: 6 -> 2", _opt.OptimisationReporter.GetReport().GenerateStringReport());
        }

        [Test]
        public void Optimiser_Registerise_For()
        {
            testEngine.Run(@"
for(var i = 0; i < 5; i += 1)
{
    print(i);
}
;");

            Assert.AreEqual("01234", testEngine.InterpreterResult);
            StringAssert.Contains("Instructions: 28 -> 18", _opt.OptimisationReporter.GetReport().GenerateStringReport());
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
            StringAssert.Contains("GOTO: 2 -> 0", _opt.OptimisationReporter.GetReport().GenerateStringReport());
        }

        [Test]
        public void Optimiser_Reorder_AutoInit_WhenSubSetMatchVarAndInitArgNames_ShouldAssignThroughAndLeaveOthersDefault()
        {
            testEngine.Run(@"
class T
{
    var a= 10, b = 20, c = 30;
    init(a,b)
    {
        print(this.a);
        print(this.b);
        print(this.c);
    }
}

var t = T(1,2);");

            Assert.AreEqual("1230", testEngine.InterpreterResult);
            StringAssert.Contains("Instructions: 25 -> 17", _opt.OptimisationReporter.GetReport().GenerateStringReport());
        }

        [Test]
        public void Optimiser_Reorder_AutoInit_WhenTwoMatchingVarAndInitArgNames_ShouldAssignThrough()
        {
            testEngine.Run(@"
class T
{
    var a,b;
    init(a,b)
    {
        print(this.a + this.b);
    }
}

var t = T(1,2);");

            Assert.AreEqual("3", testEngine.InterpreterResult);
            StringAssert.Contains("Instructions: 18 -> 11", _opt.OptimisationReporter.GetReport().GenerateStringReport());
            StringAssert.Contains("Instructions: 19 -> 13", _opt.OptimisationReporter.GetReport().GenerateStringReport());
        }

        [Test]
        public void Optimiser_WeavedGoto_ShouldReorg()
        {
            testEngine.Run(@"
        goto start;
        label mid;
        print(a);
        return;
        label start;
        var a = 1;
        goto mid;
        ");

            Assert.AreEqual("1", testEngine.InterpreterResult);
            StringAssert.Contains("Instructions: 15 -> 7", _opt.OptimisationReporter.GetReport().GenerateStringReport());   //todo we want to instead confirm that x instructions are gone
        }

        [Test]
        public void Optimiser_Reorder_Loop_WhenGivenNumberArrayAndItemName_ShouldPrintItems()
        {
            testEngine.Run(@"
        var arr = [1,2,3,];

        loop arr,j
        {
            print(jtem);
        }
        ");

            Assert.AreEqual("123", testEngine.InterpreterResult);
            StringAssert.Contains("Instructions: 67 -> 43", _opt.OptimisationReporter.GetReport().GenerateStringReport());
        }

        [Test]
        public void Optimiser_Reorder_WhileInFunc()
        {
            testEngine.Run(@"
        fun DoIt()
        {
            var i = 0;
            for(;i < 5;)
            {
                i = i + 1;
                print (i);
            }
        }

        DoIt();");

            Assert.AreEqual("12345", testEngine.InterpreterResult);
            StringAssert.Contains("Instructions: 27 -> 18", _opt.OptimisationReporter.GetReport().GenerateStringReport());
        }

        [Test]
        public void Optimiser_Reorder_While()
        {
            testEngine.Run(@"
        var i = 0;
        for(;i < 5;)
        {
            i = i + 1;
            print (i);
        }
        ;");

            Assert.AreEqual("12345", testEngine.InterpreterResult);
            StringAssert.Contains("Instructions: 28 -> 21", _opt.OptimisationReporter.GetReport().GenerateStringReport());
        }

        [Test]
        public void Optimiser_Reorder_Engine_Cycle_While_Nested_Locals()
        {
            testEngine.Run(@"
        fun DoIt()
        {
            var i = 0;
            var j = 0;
            for(;i < 5;)
            {
                j= 0;
                for(;j < 5;)
                {
                    j = j + 1;
                    print (j);
                    print (i);
                }
                i = i + 1;
                print (i);
            }
        }

        DoIt();");

            Assert.AreEqual("1020304050111213141512122232425231323334353414243444545", testEngine.InterpreterResult);
            StringAssert.Contains("Instructions: 59 -> 42", _opt.OptimisationReporter.GetReport().GenerateStringReport());
        }

        [Test]
        public void Optimiser_SubsequentLabel_CollapseAndRemoveLabel()
        {
            testEngine.Run(@"
        if(false)
            goto b;
        else
            goto a;

        goto c;
        label a;
        label b;
        label c;
        print (1+2);");

            Assert.AreEqual("3", testEngine.InterpreterResult);
            //a b and c all go to same loc, so should be removed
            StringAssert.Contains("Instructions: 27 -> 12", _opt.OptimisationReporter.GetReport().GenerateStringReport());
            StringAssert.Contains("Labels: 6 -> 2", _opt.OptimisationReporter.GetReport().GenerateStringReport());
        }

        [Test]
        public void Optimiser_SubsequentLabelTrue_CollapseAndRemoveLabel()
        {
            testEngine.Run(@"
        if(true)
            goto b;
        else
            goto a;

        goto c;
        label a;
        label b;
        label c;
        print (1+2);");

            Assert.AreEqual("3", testEngine.InterpreterResult);
            //a b and c all go to same loc, so should be removed
            StringAssert.Contains("Instructions: 27 -> 12", _opt.OptimisationReporter.GetReport().GenerateStringReport());
            StringAssert.Contains("Labels: 6 -> 2", _opt.OptimisationReporter.GetReport().GenerateStringReport());
        }

        [Test]
        public void Optimiser_Mixin_WhenCombined_ShouldHaveFlavourMethod()
        {
            testEngine.Run(@"
        class MixMe
        {
            Speak(){print(cname);}
        }

        class Foo 
        {
            mixin MixMe;
            var bar = 2;
        }

        var foo = Foo();
        foo.Speak();");

            Assert.AreEqual("MixMe", testEngine.InterpreterResult);
            StringAssert.Contains("Instructions: 6 -> 2", _opt.OptimisationReporter.GetReport().GenerateStringReport());
            StringAssert.Contains("Instructions: 15 -> 9", _opt.OptimisationReporter.GetReport().GenerateStringReport());
        }

        [Test]
        public void Optimiser_Sample_ControlFlow()
        {
            testEngine.Run(@"
        if(true)return;
        testset ControlFlow
        {
            test If
            {
                var expected = 1;
                var result = 0;
                var a = true;
                var b = true;

                if(a == b)
                    result = expected;

                Assert.AreEqual(expected, result);
            }

            test IfCompoundLogic
            {
                var expected = 1;
                var result = 0;
                var a = true;
                var b = false;
                var c = true;

                if(!(a == b == c))
                    result = expected;

                Assert.AreEqual(expected, result);
            }

            test Else
            {
                var expected = 1;
                var result = 0;
                var a = true;
                var b = false;

                if(a == b)
                    result = 0;
                else
                    result = expected;

                Assert.AreEqual(expected, result);
            }

            test ElseIf
            {
                var expected = 1;
                var result = 0;
                var a = true;
                var b = false;
                var c = true;

                if(a == b)
                    result = 0;
                else if(a == c)
                    result = expected;
                else
                    result = 0;

                Assert.AreEqual(expected, result);
            }

            test Nested
            {
                var expected = 1;
                var result = 0;
                var a = true;
                var b = true;
                var c = true;

                if(a == b)
                {
                    if(a == c)
                    {
                        result = expected;
                    }
                }
                else 
                {
                    result = 0;
                }

                Assert.AreEqual(expected, result);
            }

            test While
            {
                var expected = 1024;
                var limit = 1000;
                var accum = 1;

                for(;accum < limit;) accum += accum;

                Assert.AreEqual(expected, accum);
            }

            test For
            {
                var expected = 4951;
                var limit = 100;
                var accum = 1;

                for(var i = 0; i < limit; i += 1)
                    accum += i;

                Assert.AreEqual(expected, accum);
            }

            test ForNoDeclare
            {
                var expected = 4951;
                var limit = 100;
                var accum = 1;
                var i = 0;

                for(; i < limit; i += 1)
                    accum += i;

                Assert.AreEqual(expected, accum);
            }

            test Break
            {
                var expected = 1024;
                var limit = 1000;
                var accum = 1;

                for(;true;)
                {
                    accum += accum;
                    if(accum > limit) 
                        break;
                }

                Assert.AreEqual(expected, accum);
            }

            test Loop
            {
                var expected = 128;
                var limit = 100;
                var accum = 1;

                loop
                {
                    accum += accum;
                    if(accum > limit)
                        break;
                }

                Assert.AreEqual(expected, accum);
            }

            test ContinueWhile
            {
                var expected = 51;
                var limit = 100;
                var accum = 1;
                var i = 0;

                while(i < limit)
                {
                    i += 1;
                    if(i % 2 == 0)
                    {
                        continue;
                    }

                    accum += 1;
                }

                Assert.AreEqual(expected, accum);
            }

            test ContinueFor
            {
                var expected = 51;
                var limit = 100;
                var accum = 1;

                for(var i = 0; i < limit; i += 1)
                {
                    if(i % 2 == 0)
                    {
                        continue;
                    }

                    accum += 1;
                }

                Assert.AreEqual(expected, accum);
            }

            test ContinueLoop
            {
                var expected = 51;
                var limit = 100;
                var accum = 1;
                var i = 0;

                loop
                {
                    i += 1;

                    if(i >= limit) break;

                    if(i % 2 == 0) continue;

                    accum += 1;
                }

                Assert.AreEqual(expected, accum);
            }

            test InvalidCompareOnNonDoubleTypes
            {
                fun WillThrow()
                {
                    7 < true;
                }

                Assert.Throws(WillThrow);
            }

            test MatchSimple
            {
                var expected = 1;
                var result = 0;
                var target = 2;

                match target
                {
                    1 : result = 1;
                    2 : result = expected;
                    3 : result = 3;
                }

                Assert.AreEqual(expected, result);
            }

            test MatchExpressionCompares
            {
                var expected = 1;
                var result = 0;
                var target = 2;

                fun One {retval =1;}

                match target
                {
                    One() : result = 1;
                    1+1 : result = expected;
                    One()+1+One() : result = 3;
                }

                Assert.AreEqual(expected, result);
            }

            test MatchBlockBody
            {
                var expected = 1;
                var result1 = 0;
                var result2 = 0;
                var target = 2;

                fun One {retval =1;}

                match target
                {
                    One() : result1 = 1;
                    1+1 : 
                    {
                        result1 = expected;
                        result2 = expected;
                    }
                    One()+1+One() : result1 = 3;
                }

                Assert.AreEqual(expected, result1);
                Assert.AreEqual(expected, result2);
            }
        }
        ");

            Assert.AreEqual("", testEngine.InterpreterResult);
            Assert.IsTrue(testEngine.MyEngine.Context.Vm.TestRunner.AllPassed);
            StringAssert.Contains("Instructions: 813 -> 518", _opt.OptimisationReporter.GetReport().GenerateStringReport());
        }

        [Test]
        public void Optimiser_Remove_WhenWithinLoop_ShouldRemoveItem()
        {
            testEngine.Run(@"
var arr = [1,2,3,];

print(""pre"");

loop arr
{
    if(item % 2 == 0)
    {
        arr.Remove(item);
        i -= 1;
        icount -= 1;
    }
    else
    {
        print(item);
    }
}

print(""post"");
");

            Assert.AreEqual("pre13post", testEngine.InterpreterResult);
            StringAssert.Contains("Instructions: 103 -> 72", _opt.OptimisationReporter.GetReport().GenerateStringReport());
        }

        [Test]
        public void Optimiser_Vec2_Operations_ShouldRemoveItem()
        {
            testEngine.Run(@"
class Vec2
{
	static Lerp(x1,y1,x2,y2,p) (xOut, yOut)
	{
		xOut = x1 + (x2 - x1) * p;
		yOut = y1 + (y2 - y1) * p;
	}

	static Length(x,y)
	{
		retval = Math.Sqrt(x*x + y*y);
		if(retval != retval)
		{
			retval = 0;
		}
	}

	//TODO needs tests
	static LengthAndNormalise(xIn, yIn) (len, xOut, yOut)
	{
		len = Vec2.Length(xIn, yIn);
		if(len != 0)
		{
			xOut = xIn / len;
			yOut = yIn / len;
			return;
		}
		xOut = 0;
		yOut = 0;
	}

	//TODO needs tests
	static Dot(x1, y1, x2, y2)
	{
		retval = x1 * x2 + y1 * y2;
	}

	static Angle(x, y)
	{
		var rads = Math.Atan2(y, x);
		retval = Math.Rad2Deg(rads);
	}
}

fun DoSomeVecMath()
{
    var (ax, ay) = (1,2);
    var (bx, by) = (3,4);
    var (cx, cy) = (0,0);

    var (addx, addy) = (ax + bx, ay + by);
    var (subx, suby) = (ax - bx, ay - by);
    var (lx, ly) = Vec2.Lerp(ax, ay, bx, by, 0.5);
    var d = Vec2.Dot(ax,ay, bx, by);
    var (blen, bnx, bny) = Vec2.LengthAndNormalise(bx, by);

    print(""{ax},{ay},{bx},{by},{cx},{cy},{addx},{addy},{subx},{suby},{lx},{ly},{d},{blen},{bnx},{bny}"");
}

DoSomeVecMath();
");

            Assert.AreEqual("1,2,3,4,0,0,4,6,-2,-2,2,3,11,5,0.6,0.8", testEngine.InterpreterResult);
            StringAssert.Contains("Instructions: 119 -> 89", _opt.OptimisationReporter.GetReport().GenerateStringReport());
        }
    }
}
