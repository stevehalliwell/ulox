using NUnit.Framework;

namespace ULox.Core.Tests
{
    public class TestSetTests : EngineTestBase
    {
        [Test]
        public void Engine_Test_Empty()
        {
            testEngine.Run(@"
testset T
{
    test A
    {
    }
}"
            );

            Assert.AreEqual("", testEngine.InterpreterResult);
        }
        
        [Test]
        public void Engine_AnonTestSet_Empty()
        {
            testEngine.Run(@"
testset
{
    test A
    {
    }
}"
            );

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_AnonTestSetAndAnonTest_Empty()
        {
            testEngine.Run(@"
testset
{
    test
    {
    }
}"
            );

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Test_Simple1()
        {
            testEngine.Run(@"
testset T
{
    test A
    {
        print(2);
    }
}");

            Assert.AreEqual("2", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Test_Simple2()
        {
            testEngine.Run(@"
testset T
{
    test A
    {
        Assert.AreEqual(2,2);
    }
}");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Test_Simple3()
        {
            testEngine.Run(@"
testset T
{
    test A
    {
        var a = 2;
        var b = 3;
        Assert.AreNotEqual(a,b);
    }
}");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Test_Simple4()
        {
            testEngine.Run(@"
testset T
{
    test A
    {
        var a = 2;
        var b = 3;
        var c = a + b;
        Assert.AreEqual(c,5);
    }
}"
            );

            Assert.AreEqual("", testEngine.InterpreterResult);
            Assert.AreEqual("T:A Completed", testEngine.MyEngine.Context.Vm.TestRunner.GenerateDump());
        }

        [Test]
        public void Engine_Test_MultipleEmpty()
        {
            testEngine.Run(@"
testset T
{
    test A
    {
    }
    test B
    {
    }
}");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Test_ReportAll()
        {
            testEngine.Run(@"
testset T
{
    test A
    {
        throw;
    }
    test B
    {
    }
    test C
    {
        throw;
    }
}"
            );

            Assert.AreEqual("", testEngine.InterpreterResult);
            var completeReport = testEngine.MyEngine.Context.Vm.TestRunner.GenerateDump();
            StringAssert.Contains("T:A Incomplete", completeReport);
            StringAssert.Contains("T:B Completed", completeReport);
            StringAssert.Contains("T:C Incomplete", completeReport);
        }

        [Test]
        public void TestFxiture_WhenPrintInBodyAndTwoCases_ShouldPrintTwice()
        {
            testEngine.Run(@"
testset T
{
    print(1);
    
    test A
    {
    }
    test B
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
testset T
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
testset T
{
    var foo = 1;
    
    test A
    {
        print(foo);
    }
}"
            );

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Test_WhenDuplicate_ShouldFail()
        {
            testEngine.Run(@"
testset T
{
    test A
    {
    }
    test A
    {
    }
}"
            );

            StringAssert.StartsWith("TestRunner found a duplicate test 'T:A' at ip:", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_Test_MultipleSimple()
        {
            testEngine.Run(@"
testset T
{
    test A
    {
        var a = 1;
        var b = 2;
        var c = a + b;
        Assert.AreEqual(c,3);
    }
    test B
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
        public void Engine_Test_Simple4_Skipped()
        {
            testEngine.MyEngine.Context.Vm.TestRunner.Enabled = false;

            testEngine.Run(@"
testset T
{
    test A
    {
        var a = 2;
        var b = 3;
        var c = a + b;
        Assert.AreEqual(c,5);
    }
}"
            );

            Assert.AreEqual("", testEngine.InterpreterResult);
            Assert.AreEqual("", testEngine.MyEngine.Context.Vm.TestRunner.GenerateDump());
        }
        
        [Test]
        public void Engine_Test_ContextNames()
        {
            testEngine.Run(@"
testset Foo
{
    test Bar
    {
        print(tsname);
        print(tname);
    }
}");

            Assert.AreEqual("FooBar", testEngine.InterpreterResult);
        }

        [Test]
        public void Test_WithArgsNoData_ShouldFailCannotAddNulls()
        {
            testEngine.Run(@"
testset T
{
    test Add(lhs, rhs, expected)
    {
        var result = lhs + rhs;
        Assert.AreEqual(expected, result);
    }
}"
            );

            Assert.AreEqual("Test 'Add' has arguments but no data expression in chunk 'root(test)' at 4:33.", testEngine.InterpreterResult);
        }

        [Test]
        public void Test_WithArgsSingleDataSet_ShouldPass()
        {
            testEngine.Run(@"
var AddDataSource = [];
var first = [];
first.Add(1);
first.Add(2);
first.Add(3);
AddDataSource.Add(first);

testset T
{
    test (AddDataSource) Add(lhs, rhs, expected)
    {
        var result = lhs + rhs;
        Assert.AreEqual(expected, result);
    }
}"
            );

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Test_WithDataSetAndManuallyGrabbed_ShouldPass()
        {
            testEngine.Run(@"
var AddDataSource = [];
var first = [];
first.Add(1);
first.Add(2);
first.Add(3);
AddDataSource.Add(first);

testset T
{
    test Add()
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
        public void Test_WithArgsEmptyDataSet_ShouldPass()
        {
            testEngine.Run(@"
var AddDataSource = [];

testset T
{
    test (AddDataSource) Add(lhs, rhs, expected)
    {
        var result = lhs + rhs;
        Assert.AreEqual(expected, result);
    }
}"
            );

            Assert.AreEqual("", testEngine.InterpreterResult);
            StringAssert.DoesNotContain("T:Add", testEngine.MyEngine.Context.Vm.TestRunner.GenerateDump());
        }

        [Test]
        public void Test_WithSourceButNoArg_ShouldPassNoEffect()
        {
            testEngine.Run(@"
var source = [];
source.Add(1);

testset T
{
    test (source) IsOne()
    {
    }
}"
            );

            Assert.AreEqual("Test 'IsOne' has data expression but no arguments in chunk 'root(test)' at 7:26.", testEngine.InterpreterResult);
        }

        [Test]
        public void Test_WithSourceAndSingleArg_ShouldPass()
        {
            testEngine.Run(@"
var source = [];
source.Add(1);

testset T
{
    test (source) IsOne(val)
    {
    }
}"
            );

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Test_WithNoSourceAndNoArg_ShouldPass()
        {
            testEngine.Run(@"
var source = [];
source.Add(1);

testset T
{
    test IsOne
    {
    }
}"
            );

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Test_WithManualDataSourceSingle_ShouldPass()
        {
            testEngine.Run(@"
var source = [];
source.Add(1);

testset T
{
    test IsOne()
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
        public void Test_WithArgsMultiDataSet_ShouldPass()
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

testset T
{
    test (AddDataSource) Add(lhs, rhs, expected)
    {
        print(expected);
        var result = lhs + rhs;
        Assert.AreEqual(expected, result);
    }
}"
            );

            Assert.AreEqual("32", testEngine.InterpreterResult);
            StringAssert.DoesNotContain("Incomplete", testEngine.MyEngine.Context.Vm.TestRunner.GenerateDump());
        }

        [Test]
        public void Test_WithPrintSingleData_ShouldPass()
        {
            testEngine.Run(@"
var source = [];
source.Add(1);
source.Add(2);
source.Add(3);

testset T
{
    test (source) One(val)
    {
        print(val);
    }
}"
            );

            Assert.AreEqual("123", testEngine.InterpreterResult);
            StringAssert.DoesNotContain("Incomplete", testEngine.MyEngine.Context.Vm.TestRunner.GenerateDump());
        }

        [Test]
        public void Test_WithFixtureDataPrintSingleData_ShouldPass()
        {
            testEngine.Run(@"
testset T
{
    var source = [];
    source.Add(1);
    source.Add(2);
    source.Add(3);

    test (source) One(val)
    {
        print(val);
    }
}"
            );

            Assert.AreEqual("123", testEngine.InterpreterResult);
            StringAssert.DoesNotContain("Incomplete", testEngine.MyEngine.Context.Vm.TestRunner.GenerateDump());
        }

        [Test]
        public void Test_WithFixtureDataPrintSingleInlineData_ShouldPass()
        {
            testEngine.Run(@"
testset T
{
    test ([1,2,3]) One(val)
    {
        print(val);
    }
}"
            );

            Assert.AreEqual("123", testEngine.InterpreterResult);
            StringAssert.DoesNotContain("Incomplete", testEngine.MyEngine.Context.Vm.TestRunner.GenerateDump());
        }

        [Test]
        public void Test_InvalidLocation_ShouldFail()
        {
            testEngine.Run(@"
test ([1,2,3]) One(val)
{
    print(val);
}
"
            );
            
            StringAssert.StartsWith("Unexpected test, it can only appear within a testset,", testEngine.InterpreterResult);
        }

        [Test]
        public void Test_WithArgsAndInlineMultiDataSet_ShouldPass()
        {
            testEngine.Run(@"
testset T
{
    test ([ [1,2,3,], [1,1,2] ]) Add(lhs, rhs, expected)
    {
        print(expected);
        var result = lhs + rhs;
        Assert.AreEqual(expected, result);
    }
}"
            );

            Assert.AreEqual("32", testEngine.InterpreterResult);
            StringAssert.DoesNotContain("Incomplete", testEngine.MyEngine.Context.Vm.TestRunner.GenerateDump());
        }
    }
}
