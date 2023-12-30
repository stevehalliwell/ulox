using NUnit.Framework;

namespace ULox.Core.Tests
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
var map = [:];
print(map);
");

            Assert.AreEqual("<inst NativeMap>", testEngine.InterpreterResult);
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
        public void Map_ReadOnlyThenSet_Error()
        {
            testEngine.Run(@"
var map = [:];
readonly map;
map[1] = 2;
");

            StringAssert.StartsWith("Attempted to Set index '1' to '2'", testEngine.InterpreterResult);
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
        public void Engine_MapByNameThenSet_MatchesValue()
        {
            testEngine.Run(@"
var map = Map();
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

        [Test]
        public void Invalid_WhenMixingListThenMap_ShouldFail()
        {
            testEngine.Run(@"
var map = [3,4,""a"":2,];
print(map[""a""]);
");

            Assert.AreEqual("Expected to compile Expression, but encountered error in chunk 'unnamed_chunk(test)' at 2:20.", testEngine.InterpreterResult);
        }

        [Test]
        public void CreateOrUpdate_Chained_ShouldHaveExpected()
        {
            testEngine.Run(@"
var d = Map()
    .CreateOrUpdate(1,1)
    .CreateOrUpdate(2,2);

print(d[1]);
print(d[2]);
");

            Assert.AreEqual("12", testEngine.InterpreterResult);
        }
    }
}
