using NUnit.Framework;

namespace ulox.core.tests
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
        public void Map_WhenInlineCreate_ShouldMatchesValue()
        {
            testEngine.Run(@"
var map = [""a"":2, ""b"":5, ""c"":{a:1},];
print(map[""a""]);
print(map[""b""]);
print(map[""c""].a);
");

            Assert.AreEqual("251", testEngine.InterpreterResult);
        }

        [Test]
        public void Invalid_WhenMixingMapThenlist_ShouldFail()
        {
            testEngine.Run(@"
var map = [""a"":2,3,4];
print(map[""a""]);
");

            Assert.AreEqual("Expect ':' after key in source 'test' at 2:19 '3'.", testEngine.InterpreterResult);
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
    }
}
