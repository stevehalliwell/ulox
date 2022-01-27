using NUnit.Framework;

namespace ULox.Tests
{
    public class MapUsageTests : EngineTestBase
    {
        [Test]
        public void Engine_MapEmtpy_Count0()
        {
            testEngine.Run(@"
var map = [:];
print(map.Count());
");

            Assert.AreEqual("0", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_NativeMap_Create()
        {
            testEngine.Run(@"
var arr = [:];
print(arr);
");

            Assert.AreEqual("<inst NativeMapClass>", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_MapSet_Count1()
        {
            testEngine.Run(@"
var map = [:];
map[1] = 2;
print(map.Count());
");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_MapSet_MatchesValue()
        {
            testEngine.Run(@"
var map = [:];
map[1] = 2;
print(map[1]);
");

            Assert.AreEqual("2", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_MapSetAndUpdate_MatchesValue()
        {
            testEngine.Run(@"
var map = [:];
map[""a""] = 2;
map[""a""] = 5;
print(map[""a""]);
");

            Assert.AreEqual("5", testEngine.InterpreterResult);
        }
    }
}
