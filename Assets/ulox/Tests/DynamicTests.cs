using NUnit.Framework;

namespace ULox.Tests
{
    public class DynamicTests : EngineTestBase
    {
        [Test]
        public void Engine_Dynamic()
        {
            testEngine.AddLibrary(new StandardClassesLibrary());

            testEngine.Run(@"
var obj = Dynamic();

obj.a = 1;
obj.b = 2;
obj.c = 3;
obj.d = -1;

var d = obj.a + obj.b + obj.c;
obj.d = d;

print(obj.d);
");

            Assert.AreEqual("6", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_DynamicType_SameFunc()
        {
            testEngine.Run(@"
fun AddPrint(obj)
{
    print(obj.a + obj.b);
}

class T1
{
    var z,a=1,b=2;
}
class T2
{
    var x,y,z,a=""Hello "",b=""World"";
}
class T3
{
}

var t1 = T1();
var t2 = T2();
var t3 = T3();
t3.a = 1;
t3.b = 1;

AddPrint(t1);
AddPrint(t2);
AddPrint(t3);
");

            Assert.AreEqual("3Hello World2", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_DynamicType_SameInvoke()
        {
            testEngine.Run(@"
fun AddPrint(obj)
{
    obj.DoTheThing();
}

class T1
{
    var z,a=1,b=2;
    DoTheThing()
    {
        print (this.a + this.b);
    }
}
class T2
{
    var z,a=1,b=2;
    DoSomeOtherThing()
    {
        throw;
    }

    DoTheThing()
    {
        print (this.a + this.b);
    }
}

var t1 = T1();
var t2 = T2();

AddPrint(t1);
AddPrint(t2);
");

            Assert.AreEqual("33", testEngine.InterpreterResult);
        }
    }
}
