using NUnit.Framework;

namespace ULox.Tests
{
    public class ListUsageTests : EngineTestBase
    {
        [Test]
        public void Engine_List()
        {
            testEngine.MyEngine.Context.AddLibrary(new StandardClassesLibrary());

            testEngine.Run(@"
var list = [];

for(var i = 0; i < 5; i += 1)
    list.Add(i);

var c = list.Count();
print(c);

for(var i = 0; i < c; i += 1)
    print(list[i]);

for(var i = 0; i < c; i +=1)
    list[i] = -i;

for(var i = 0; i < c; i += 1)
    print(list[i]);
");

            Assert.AreEqual("5012340-1-2-3-4", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_NativeList_Create()
        {
            testEngine.Run(@"
var arr = [];
print(arr);
");

            Assert.AreEqual("<inst NativeListClass>", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_NativeList_Count()
        {
            testEngine.Run(@"
var arr = [];
print(arr.Count());
");

            Assert.AreEqual("0", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_NativeList_Add_CountInc()
        {
            testEngine.Run(@"
var arr = [];
arr.Add(1);
print(arr.Count());
");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_NativeList_Add_ValueMatches()
        {
            testEngine.Run(@"
var arr = [];
arr.Add(1);
print(arr[0]);
");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_NativeList_AddThenSet_NewValueMatches()
        {
            testEngine.Run(@"
var arr = [];
arr.Add(1);
arr[0] = 2;
print(arr[0]);
");

            Assert.AreEqual("2", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_NativeList_SameObjectAddThenRemove_CountIs0()
        {
            testEngine.Run(@"
var arr = [];
var obj = 1;
arr.Add(obj);
arr.Remove(obj);
print(arr.Count());
");

            Assert.AreEqual("0", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_NativeList_SameValueAddThenRemove_CountIs0()
        {
            testEngine.Run(@"
var arr = [];
arr.Add(2);
arr.Remove(2);
print(arr.Count());
");

            Assert.AreEqual("0", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_NativeList_Resize10_CountIs0()
        {
            testEngine.Run(@"
var arr = [];
arr.Resize(10, null);
print(arr.Count());
");

            Assert.AreEqual("10", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_NativeList_SetNonTrivial_ValueMatches()
        {
            testEngine.Run(@"
var arr = [];
arr.Resize(10, null);
arr[5] = 2*4;
print(arr[5]);
");

            Assert.AreEqual("8", testEngine.InterpreterResult);
        }
    }
}
