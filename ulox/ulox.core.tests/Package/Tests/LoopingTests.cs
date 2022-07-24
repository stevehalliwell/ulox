﻿using NUnit.Framework;

namespace ULox.Tests
{
    [TestFixture]
    public class LoopingTests : EngineTestBase
    {
        [Test]
        public void Loop_WhenBreakAt5_ShouldPrintUpTo6()
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
        }

        [Test]
        public void Loop_WhenNoEscape_ShouldNotCompile()
        {
            testEngine.Run(@"
var i = 0;
loop
{
    print (i);
    i = i + 1;
}");

            Assert.AreEqual("Loops must contain an termination.", testEngine.InterpreterResult);
        }

        [Test]
        public void While_WhenIncrementLimited_ShouldIterateTwice()
        {
            testEngine.Run(@"
var i = 0;
while(i < 2)
{
    print (""hip, "");
    i = i + 1;
}

print (""hurray"");");

            Assert.AreEqual("hip, hip, hurray", testEngine.InterpreterResult);
        }

        [Test]
        public void For_WhenLimitedIterations_ShouldPrint3Times()
        {
            testEngine.Run(@"
for(var i = 0; i < 2; i = i + 1)
{
    print(""hip, "");
}

print(""hurray"");
");

            Assert.AreEqual("hip, hip, hurray", testEngine.InterpreterResult);
        }

        [Test]
        public void Break_WhenBreakOutOnFirstLoop_ShouldBreakOnFirst()
        {
            testEngine.Run(@"
var i = 0;
while(i < 5)
{
    print (""success?"");
    break;
    print (""FAIL"");
    print(""FAIL"");
    print(""FAIL"");
    print(""FAIL"");
    i = i + 1;
}

print (""hurray"");");

            Assert.AreEqual("success?hurray", testEngine.InterpreterResult);
        }

        [Test]
        public void Continue_WhenHit_ShouldReturnToStartOfLoop()
        {
            testEngine.Run(@"
var i = 0;
while(i < 3)
{
    i = i + 1;
    print (i);
    continue;
    print (""FAIL"");
}");

            Assert.AreEqual("123", testEngine.InterpreterResult);
        }

        [Test]
        public void While_WhenNested_ShouldNSquared()
        {
            testEngine.Run(@"
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
}");

            Assert.AreEqual("1020304050111213141512122232425231323334353414243444545", testEngine.InterpreterResult);
        }

        [Test]
        public void While_WhenNestedAndInnerCounterDeclare_ShouldNSquared()
        {
            testEngine.Run(@"
var i = 0;
while(i < 5)
{
    var j = 0;
    while(j < 5)
    {
        j = j + 1;
        print (j);
        print (i);
    }
    i = i + 1;
    print (i);
}");

            Assert.AreEqual("1020304050111213141512122232425231323334353414243444545", testEngine.InterpreterResult);
        }

        [Test]
        public void For_WhenContinued_ShouldMoveToIterationStep()
        {
            testEngine.Run(@"
for(var i = 0;i < 3; i += 1)
{
    print (i);
    continue;
    print (""FAIL"");
}");

            Assert.AreEqual("012", testEngine.InterpreterResult);
        }

        [Test]
        public void For_WhenContinuedAndExternalDeclaredI_ShouldMoveToIterationStep()
        {
            testEngine.Run(@"
var i = 0;
for(i = 0;i < 3; i += 1)
{
    print (i);
    continue;
    print (""FAIL"");
}");

            Assert.AreEqual("012", testEngine.InterpreterResult);
        }

        [Test]
        public void For_WhenContinuedAndExternalDeclaredAndAssignedI_ShouldMoveToIterationStep()
        {
            testEngine.Run(@"
var i = 0;
for(;i < 3; i += 1)
{
    print (i);
    continue;
    print (""FAIL"");
}");

            Assert.AreEqual("012", testEngine.InterpreterResult);
        }

        [Test]
        public void Loop_WhenGivenEmptyNativeArray_ShouldTerminate()
        {
            testEngine.Run(@"
var arr = [];

loop (arr)
{
    print(""FAIL"");
    break;
}
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Loop_WhenGivenNumberArray_ShouldPrintIndicies()
        {
            testEngine.Run(@"
var arr = [];
arr.Add(""a"");
arr.Add(""b"");
arr.Add(""c"");

loop (arr)
{
    print(i);
}
");

            Assert.AreEqual("012", testEngine.InterpreterResult);
        }

        [Test]
        public void Loop_WhenGivenNumberArray_ShouldPrintItems()
        {
            testEngine.Run(@"
var arr = [];
arr.Add(""a"");
arr.Add(""b"");
arr.Add(""c"");

loop (arr)
{
    print(item);
}
");

            Assert.AreEqual("abc", testEngine.InterpreterResult);
        }

        [Test]
        public void Loop_WhenGivenNumberArrayAndCustomNames_ShouldPrintIndicies()
        {
            testEngine.Run(@"
var arr = [];
arr.Add(""a"");
arr.Add(""b"");
arr.Add(""c"");

loop (arr, jtem, j)
{
    print(j);
}
");

            Assert.AreEqual("012", testEngine.InterpreterResult);
        }

        [Test]
        public void Loop_WhenGivenNumberArrayAndItemName_ShouldPrintItems()
        {
            testEngine.Run(@"
var arr = [];
arr.Add(""a"");
arr.Add(""b"");
arr.Add(""c"");

loop (arr,jtem)
{
    print(jtem);
}
");

            Assert.AreEqual("abc", testEngine.InterpreterResult);
        }
    }
}
