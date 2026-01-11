using NUnit.Framework;

namespace ULox.Core.Tests
{
    public class HotLoadDataTests : EngineTestBase
    {
        [Test]
        public void Reload_DataOnly()
        {
            double GetAValue()
            {
                testEngine.MyEngine.Context.Vm.Globals.Get(new HashedString("obj"), out var obj);
                obj.val.asInstance.Fields.Get(new HashedString("a"), out var a);
                return a.val.asDouble;
            }
            
            testEngine.Run(@"
var obj = 
{
    a = 1,
    b = 2,
    c = 3,
    d = -1,
};
");
            Assert.AreEqual("", testEngine.InterpreterResult);
            Assert.AreEqual(1, GetAValue());

            testEngine.Run(@"
var obj =
{
    a = 4,
    b = 5,
    c = 6,
    d = -1,
};
");
            Assert.AreEqual("", testEngine.InterpreterResult);
            Assert.AreEqual(4, GetAValue());
        }

        [Test]
        public void Reload_Types_ThenDataOnly()
        {
            double GetAValue()
            {
                testEngine.MyEngine.Context.Vm.Globals.Get(new HashedString("obj"), out var obj);
                obj.val.asInstance.Fields.Get(new HashedString("a"), out var a);
                return a.val.asDouble;
            }

            testEngine.Run(@"
class MyData
{
var 
    a = 1,
    b = 2,
    c = 3,
    d = -1,
;
}

fun TraverseUpdateAssigner(lhs, rhs)
{
    retval = rhs;
}

fun TraverseUpdate(lhs, rhs)
{
    retval = Object.TraverseUpdate(lhs, rhs, TraverseUpdateAssigner);
}
");
            Assert.AreEqual("", testEngine.InterpreterResult);

            testEngine.Run(@"
var obj = TraverseUpdate(MyData(), {a=9});
");
            Assert.AreEqual("", testEngine.InterpreterResult);
            Assert.AreEqual(9, GetAValue());

            testEngine.Run(@"
var obj = TraverseUpdate(MyData(), {a=8});
");
            Assert.AreEqual("", testEngine.InterpreterResult);
            Assert.AreEqual(8, GetAValue());
        }

        //define some types and data, repase the whole lot

        [Test]
        public void Reload_TypesAndData()
        {
            double GetAValue()
            {
                testEngine.MyEngine.Context.Vm.Globals.Get(new HashedString("obj"), out var obj);
                obj.val.asInstance.Fields.Get(new HashedString("a"), out var a);
                return a.val.asDouble;
            }
            testEngine.MyEngine.Context.Program.Compiler.TypeInfo.AllowTypeReplacement = true;
            testEngine.Run(@"
class MyData
{
var 
    a = 1,
    b = 2,
    c = 3,
    d = -1,
;
}

fun TraverseUpdateAssigner(lhs, rhs)
{
    retval = rhs;
}

fun TraverseUpdate(lhs, rhs)
{
    retval = Object.TraverseUpdate(lhs, rhs, TraverseUpdateAssigner);
}

var obj = TraverseUpdate(MyData(), {a=9});
");
            Assert.AreEqual("", testEngine.InterpreterResult);
            Assert.AreEqual(9, GetAValue());

            testEngine.Run(@"
class MyData
{
var 
    a = 1,
    b = 2,
    c = 3,
    d = -1,
;
}

fun TraverseUpdateAssigner(lhs, rhs)
{
    retval = rhs;
}

fun TraverseUpdate(lhs, rhs)
{
    retval = Object.TraverseUpdate(lhs, rhs, TraverseUpdateAssigner);
}

var obj = TraverseUpdate(MyData(), {a=8});
");
            Assert.AreEqual("", testEngine.InterpreterResult);
            Assert.AreEqual(8, GetAValue());
        }
    }
}