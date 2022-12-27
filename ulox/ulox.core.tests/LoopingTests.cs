using NUnit.Framework;

namespace ulox.core.tests
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

            Assert.AreEqual("Loops must contain a termination in chunk 'unnamed_chunk(test)' at 7:2.", testEngine.InterpreterResult);
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

        [Test]
        public void LoopNested_WhenGivenNumberArrayAndItemName_ShouldPrintItems()
        {
            testEngine.Run(@"
var arr = [];
arr.Add(""a"");
arr.Add(""b"");
arr.Add(""c"");

loop (arr,jtem, j, jount)
{
    print(jtem);
    loop(arr)
    {
        print(item);
    }
}
");

            Assert.AreEqual("aabcbabccabc", testEngine.InterpreterResult);
        }

        [Test]
        public void Loop_WhenGivenNull_ShouldSkipLoop()
        {
            testEngine.Run(@"
var arr = null;

loop (arr)
{
    print(""inner"");
}
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Loop_WhenGivenNumber_ShouldFailToInvoke()
        {
            testEngine.Run(@"
var arr = 7;

loop (arr)
{
}
");

            StringAssert.StartsWith("Cannot perform countof on '7' at ip:", testEngine.InterpreterResult);
        }

        [Test]
        public void Loop_WhenGivenString_ShouldFailToInvoke()
        {
            testEngine.Run(@"
var arr = ""str"";

loop (arr)
{
}
");

            StringAssert.StartsWith("Cannot perform countof on 'str' at ip:", testEngine.InterpreterResult);
        }

        [Test]
        public void Loop_WhenGivenEmptyMap_ShouldDoNothing()
        {
            testEngine.Run(@"
var map = [:];

loop (map)
{
    print(i);
}

print(""Pass"");
");

            Assert.AreEqual("Pass", testEngine.InterpreterResult);
        }

        [Test]
        public void Loop_WhenGivenNonEmptyMap_ShouldFail()
        {
            testEngine.Run(@"
var map = [:];
map[""nothing""] = ""something"";

loop (map)
{
    print(item);
}

print(""Pass"");
");

            Assert.AreEqual("Map contains no key of '0'.", testEngine.InterpreterResult);
        }

        [Test]
        public void Loop_WhenGivenNonEmptyMapWithValidNumberKey_ShouldPass()
        {
            testEngine.Run(@"
var map = [:];
map[0] = ""something"";

loop (map)
{
    print(item);
}
");

            Assert.AreEqual("something", testEngine.InterpreterResult);
        }

        [Test]
        public void Loop_WhenGivenStub_ShouldFailToInvoke()
        {
            testEngine.Run(@"
class Stub {}
var arr = Stub();

loop (arr)
{
}
");

            StringAssert.StartsWith("Cannot perform countof on '<inst Stub>' at ip:", testEngine.InterpreterResult);
        }

        [Test]
        public void CountOf_WhenGivenFakeList_ShouldMatch()
        {
            testEngine.Run(@"
class FakeList { _co(self) { return 1; } }
var arr = FakeList();

print(countof arr);
");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Loop_WhenGivenFakeList_ShouldFailToInvoke()
        {
            testEngine.Run(@"
class FakeList { _co(self) { return 1; } }
var arr = FakeList();

print(countof arr);

loop (arr)
{
}
");

            StringAssert.StartsWith("1Cannot perform get index on type 'Instance' at ip:", testEngine.InterpreterResult);
        }

        [Test]
        public void Loop_WhenGivenCustomList_ShouldPass()
        {
            testEngine.Run(@"
class FakeList 
{ 
    _co(self) { return 1; }
    _gi(self, i) { return ""Hello""; }
}
var arr = FakeList();

loop (arr)
{
    print(item);
}
");

            Assert.AreEqual("Hello", testEngine.InterpreterResult);
        }

        [Test]
        public void Remove_WhenWithinLoop_ShouldRemoveItem()
        {
            testEngine.Run(@"
var arr = [];
arr.Add(1);
arr.Add(2);
arr.Add(3);

print(arr.Count());

loop (arr)
{
    if(item % 2 == 0)
    {
        arr.Remove(item);
        i -= 1;
        count -= 1;
    }
    else
    {
        print(item);
    }
}

print(arr.Count());
");

            Assert.AreEqual("3132", testEngine.InterpreterResult);
        }

        [Test]
        public void Loop_WhenIndexAlreadyInScope_ShouldThrow()
        {
            testEngine.Run(@"
{
var arr = [];
arr.Add(1);
var i = 7;

loop (arr)
{
}
}");

            StringAssert.StartsWith("Loop index name 'i' already exists at this scope in chunk 'unnamed_chunk(test)' at 7:10", testEngine.InterpreterResult);
        }

        [Test]
        public void Loop_WhenItemAlreadyInScope_ShouldThrow()
        {
            testEngine.Run(@"
{
var arr = [];
arr.Add(1);
var item = 7;

loop (arr)
{
}
}");

            StringAssert.StartsWith("Loop item name 'item' already exists at this scope", testEngine.InterpreterResult);
        }

        [Test]
        public void Loop_WhenCustomIndexAlreadyInScope_ShouldThrow()
        {
            testEngine.Run(@"
{
var arr = [];
arr.Add(1);
var j = 7;

loop (arr, jtem, j)
{
}
}");

            StringAssert.StartsWith("Loop index name 'j' already exists at this scope", testEngine.InterpreterResult);
        }

        [Test]
        public void Loop_WhenCustomItemAlreadyInScope_ShouldThrow()
        {
            testEngine.Run(@"
{
var arr = [];
arr.Add(1);
var jtem = 7;

loop (arr, jtem)
{
}
}");

            StringAssert.StartsWith("Loop item name 'jtem' already exists at this scope in chunk", testEngine.InterpreterResult);
        }

        [Test]
        public void Loop_WhenNested_ShouldPrintExpected()
        {
            testEngine.Run(@"
{
var arrays = [
            [1,2,3,4,5,],
            [6,7,8,9,0,],
        ];


var arr = [];
arr.Add(1);
var jtem = 7;

loop (arrays,ytem,y)
{
    loop(ytem, xtem, x, xount)
    {
        print(xtem);
    }
}
}
");

            Assert.AreEqual("1234567890", testEngine.InterpreterResult);
        }

        [Test]
        public void Loop_WhenGivenNumberArrayAndItemNameAndPrintCount_ShouldPrintItems()
        {
            testEngine.Run(@"
var arr = [""a"",""b"",""c"",];

loop (arr)
{
    print(item);
    print(count);
}
");

            Assert.AreEqual("a3b3c3", testEngine.InterpreterResult);
        }

        [Test]
        public void Loop_WhenGivenNumberArrayAndItemNameAndNamedCount_ShouldPrintItems()
        {
            testEngine.Run(@"
var arr = [];
arr.Add(""a"");
arr.Add(""b"");
arr.Add(""c"");

loop (arr,jtem,j,jount)
{
    print(jtem);
    print(jount);
}
");

            Assert.AreEqual("a3b3c3", testEngine.InterpreterResult);
        }

        [Test]
        public void Loop_WhenDecreaseCount_ShouldPrintItems()
        {
            testEngine.Run(@"
var arr = [""a"",""b"",""c"",];

loop (arr)
{
    print(item);
    count -= 1;
    print(count);
}
");

            Assert.AreEqual("a2b1", testEngine.InterpreterResult);
        }

        [Test]
        public void Loop_WhenNestedWithExstingLocals_ShouldPrintItems()
        {
            testEngine.Run(@"
var arr = [""a"",""b"",""c"",];

var b = 7;
var someObj = {a:1,c:10,d:{a:1,},};

loop (arr)
{
    loop(arr, ji, j, jount)
    {
        print(i+ji);
    }
}");

            Assert.AreEqual("0a0b0c1a1b1c2a2b2c", testEngine.InterpreterResult);
        }

        [Test]
        public void Loop_WhenInFunWithArgCollection_ShouldPrintItems()
        {
            testEngine.Run(@"
fun FromRowCol(outer)
{
    loop(outer, it, index, c)
    {
        var inner = ""hi"";
        print(inner);
    }
}

var outer = [].Grow(2, null);
var posList = FromRowCol(outer);
");

            Assert.AreEqual("hihi", testEngine.InterpreterResult);
        }
    }
}
