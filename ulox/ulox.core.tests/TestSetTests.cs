﻿using System;
using NUnit.Framework;

namespace ulox.core.tests
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
        public void TestFxiture_WhenPrintInBodyAndTwoCases_ShouldPrintTwice()
        {
            testEngine.Run(@"
test T
{
    print(1);
    
    testcase A
    {
    }
    testcase B
    {
    }
}"
            );

            Assert.AreEqual("11", testEngine.InterpreterResult);
        }

        [Test]
        public void TestFxiture_WhenPrintInBodyAndNoCases_ShouldNotPrint()
        {
            testEngine.Run(@"
test T
{
    print(1);
}"
            );

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void TestLocal_WhenPrinted_ShouldMatch()
        {
            testEngine.Run(@"
test T
{
    var foo = 1;
    
    testcase A
    {
        print(foo);
    }
}"
            );

            Assert.AreEqual("1", testEngine.InterpreterResult);
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

        [Test]
        public void TestCase_WithArgsNoData_ShouldFailCannotAddNulls()
        {
            testEngine.Run(@"
test T
{
    testcase Add(lhs, rhs, expected)
    {
        var result = lhs + rhs;
        Assert.AreEqual(expected, result);
    }
}"
            );

            Assert.AreEqual("Testcase 'Add' has arguments but no data expression in chunk 'unnamed_chunk(test)' at 4:43.", testEngine.InterpreterResult);
        }

        [Test]
        public void TestCase_WithArgsSingleDataSet_ShouldPass()
        {
            testEngine.Run(@"
var AddDataSource = [];
var first = [];
first.Add(1);
first.Add(2);
first.Add(3);
AddDataSource.Add(first);

test T
{
    testcase (AddDataSource) Add(lhs, rhs, expected)
    {
        var result = lhs + rhs;
        Assert.AreEqual(expected, result);
    }
}"
            );

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void TestCase_WithDataSetAndManuallyGrabbed_ShouldPass()
        {
            testEngine.Run(@"
var AddDataSource = [];
var first = [];
first.Add(1);
first.Add(2);
first.Add(3);
AddDataSource.Add(first);

test T
{
    testcase Add()
    {
        var testDataRow = AddDataSource[0];
        var lhs = testDataRow[0];
        var rhs = testDataRow[1];
        var expected = testDataRow[2];
        var result = lhs + rhs;
        Assert.AreEqual(expected, result);
    }
}"
            );

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void TestCase_WithArgsEmptyDataSet_ShouldPass()
        {
            testEngine.Run(@"
var AddDataSource = [];

test T
{
    testcase (AddDataSource) Add(lhs, rhs, expected)
    {
        var result = lhs + rhs;
        Assert.AreEqual(expected, result);
    }
}"
            );

            Assert.AreEqual("", testEngine.InterpreterResult);
            StringAssert.DoesNotContain("T:Add", testEngine.MyEngine.Context.VM.TestRunner.GenerateDump());
        }

        [Test]
        public void TestCase_WithSourceButNoArg_ShouldPassNoEffect()
        {
            testEngine.Run(@"
var source = [];
source.Add(1);

test T
{
    testcase (source) IsOne()
    {
    }
}"
            );

            Assert.AreEqual("Testcase 'IsOne' has data expression but no arguments in chunk 'unnamed_chunk(test)' at 7:35.", testEngine.InterpreterResult);
        }

        [Test]
        public void TestCase_WithSourceAndSingleArg_ShouldPass()
        {
            testEngine.Run(@"
var source = [];
source.Add(1);

test T
{
    testcase (source) IsOne(val)
    {
    }
}"
            );

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void TestCase_WithNoSourceAndNoArg_ShouldPass()
        {
            testEngine.Run(@"
var source = [];
source.Add(1);

test T
{
    testcase IsOne
    {
    }
}"
            );

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void TestCase_WithManualDataSourceSingle_ShouldPass()
        {
            testEngine.Run(@"
var source = [];
source.Add(1);

test T
{
    testcase IsOne()
    {
        var testDataRow = null;
        var testDataSource = source;
        testDataRow = testDataSource[0];
    }
}"
            );

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void TestCase_WithArgsMultiDataSet_ShouldPass()
        {
            testEngine.Run(@"
var AddDataSource = [];
var first = [];
first.Add(1);
first.Add(2);
first.Add(3);
AddDataSource.Add(first);
var second = [];
second.Add(1);
second.Add(1);
second.Add(2);
AddDataSource.Add(second);

test T
{
    testcase (AddDataSource) Add(lhs, rhs, expected)
    {
        print(expected);
        var result = lhs + rhs;
        Assert.AreEqual(expected, result);
    }
}"
            );

            Assert.AreEqual("32", testEngine.InterpreterResult);
            StringAssert.DoesNotContain("Incomplete", testEngine.MyEngine.Context.VM.TestRunner.GenerateDump());
        }

        [Test]
        public void TestCase_WithPrintSingleData_ShouldPass()
        {
            testEngine.Run(@"
var source = [];
source.Add(1);
source.Add(2);
source.Add(3);

test T
{
    testcase (source) One(val)
    {
        print(val);
    }
}"
            );

            Assert.AreEqual("123", testEngine.InterpreterResult);
            StringAssert.DoesNotContain("Incomplete", testEngine.MyEngine.Context.VM.TestRunner.GenerateDump());
        }

        [Test]
        public void TestCase_WithFixtureDataPrintSingleData_ShouldPass()
        {
            testEngine.Run(@"
test T
{
    var source = [];
    source.Add(1);
    source.Add(2);
    source.Add(3);

    testcase (source) One(val)
    {
        print(val);
    }
}"
            );

            Assert.AreEqual("123", testEngine.InterpreterResult);
            StringAssert.DoesNotContain("Incomplete", testEngine.MyEngine.Context.VM.TestRunner.GenerateDump());
        }

        [Test]
        public void TestCase_WithFixtureDataPrintSingleInlineData_ShouldPass()
        {
            testEngine.Run(@"
test T
{
    testcase ([1,2,3]) One(val)
    {
        print(val);
    }
}"
            );

            Assert.AreEqual("123", testEngine.InterpreterResult);
            StringAssert.DoesNotContain("Incomplete", testEngine.MyEngine.Context.VM.TestRunner.GenerateDump());
        }

        [Test]
        public void TestCase_WithArgsAndInlineMultiDataSet_ShouldPass()
        {
            testEngine.Run(@"
test T
{
    testcase ([ [1,2,3,], [1,1,2] ]) Add(lhs, rhs, expected)
    {
        print(expected);
        var result = lhs + rhs;
        Assert.AreEqual(expected, result);
    }
}"
            );

            Assert.AreEqual("32", testEngine.InterpreterResult);
            StringAssert.DoesNotContain("Incomplete", testEngine.MyEngine.Context.VM.TestRunner.GenerateDump());
        }
    }
}