using NUnit.Framework;

namespace ULox.Core.Tests
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
        public void For_WhenBreakAt5_ShouldPrintUpTo6()
        {
            testEngine.Run(@"
var i = 0;
for(;;)
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
        public void For_WhenNoEscape_ShouldNotCompile()
        {
            testEngine.Run(@"
var i = 0;
for(;;)
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
        public void Loop_WhenGivenEmptyNativeArray_ShouldTerminate()
        {
            testEngine.Run(@"
var arr = [];

loop arr
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

loop arr
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
var arr = [1,2,3];

loop arr
{
    print(item);
}");

            Assert.AreEqual("123", testEngine.InterpreterResult);
        }

        [Test]
        public void For_WhenGivenNumberArray_ShouldPrintItems()
        {
            testEngine.Run(@"
var arr = [1,2,3];

for(var i = 0; i < arr.Count(); i += 1)
{
    var item = arr[i];
    print(item);
}
");

            Assert.AreEqual("123", testEngine.InterpreterResult);
        }

        [Test]
        public void Loop_WhenGivenNumberArrayAndCustomNames_ShouldPrintIndicies()
        {
            testEngine.Run(@"
var arr = [];
arr.Add(""a"");
arr.Add(""b"");
arr.Add(""c"");

loop arr, jtem, j
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

loop arr,jtem
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

loop arr,jtem, j, jount
{
    print(jtem);
    loop arr
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

loop arr
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

loop arr
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

loop arr
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

loop map
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

loop map
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

loop map
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

loop arr
{
}
");

            StringAssert.StartsWith("Cannot perform countof on '<inst Stub>' at ip:", testEngine.InterpreterResult);
        }

        [Test]
        public void CountOf_WhenGivenFakeList_ShouldMatch()
        {
            testEngine.Run(@"
class FakeList { _co(self) { retval = 1; } }
var arr = FakeList();

print(countof arr);
");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Loop_WhenGivenFakeList_ShouldFailToInvoke()
        {
            testEngine.Run(@"
class FakeList { _co(self) { retval = 1; } }
var arr = FakeList();

print(countof arr);

loop arr
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
    _co(self) { retval = 1; }
    _gi(self, i) { retval = ""Hello""; }
}
var arr = FakeList();

loop arr
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

loop arr
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

loop arr
{
}
}");

            StringAssert.StartsWith("Cannot declare var with name 'i'", testEngine.InterpreterResult);
        }

        [Test]
        public void Loop_WhenItemAlreadyInScope_ShouldThrow()
        {
            testEngine.Run(@"
{
var arr = [];
arr.Add(1);
var item = 7;

loop arr
{
}
}");

            StringAssert.StartsWith("Cannot declare var with name 'item'", testEngine.InterpreterResult);
        }

        [Test]
        public void Loop_When2Siblings_ShouldCompile()
        {
            testEngine.Run(@"
{
    var arr = [];

    loop arr
    {
    }

    loop arr
    {
    }
}");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void For_When2Siblings_ShouldCompile()
        {
            testEngine.Run(@"
{
    var arr = [];

    if(arr)
    {
        var count = countof arr;
        if(count > 0)
        {
            var i = 0;
            var item = arr[i];
            for(; i < count; i += 1)
            {
                item = arr[i];
            }
        }
    }

    if(arr)
    {
        var count = countof arr;
        if(count > 0)
        {
            var i = 0;
            var item = arr[i];
            for(; i < count; i += 1)
            {
                item = arr[i];
            }
        }
    }
}");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Loop_WhenCustomIndexAlreadyInScope_ShouldThrow()
        {
            testEngine.Run(@"
{
var arr = [];
arr.Add(1);
var j = 7;

loop arr, jtem, j
{
}
}");

            StringAssert.StartsWith("Cannot declare var with name 'j'", testEngine.InterpreterResult);
        }

        [Test]
        public void Loop_WhenCustomItemAlreadyInScope_ShouldThrow()
        {
            testEngine.Run(@"
{
var arr = [];
arr.Add(1);
var jtem = 7;

loop arr, jtem
{
}
}");

            StringAssert.StartsWith("Cannot declare var with name 'jtem'", testEngine.InterpreterResult);
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

loop arrays,ytem,y
{
    loop ytem, xtem, x, xount
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

loop arr
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

loop arr,jtem,j,jount
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

loop arr
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
var someObj = {a=1,c=10,d={a=1,},};

loop arr
{
    loop arr, jtem, j, jount
    {
        print(i+jtem);
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
    loop outer, it, index, c
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

        //todo: we want this to work but it requires a change in loopstatement 
        // the desugar would need to change so that first finds the end of the arr expression
        //  assigns that to a new unique local var, and then use that new var from then on
        //[Test]
        //public void Loop_WhenGivenInstanceFieldIdentifier_ShouldPrintItems()
        //{
        //    testEngine.Run(@"
        //var thing = {=};
        //thing.arr = [""a"",""b"",""c"",];

        //loop (thing.arr)
        //{
        //    print(item);
        //}");

        //    Assert.AreEqual("abc", testEngine.InterpreterResult);
        //}



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
        public void For_WhenUsedAsWhile_ShouldCompile()
        {
            testEngine.Run(@"
var i = 0;
for(;i < 3;)
{
    i = i + 1;
    print (i);
    continue;
    print (""FAIL"");
}");

            Assert.AreEqual("123", testEngine.InterpreterResult);
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
        public void Loop_WhenContinued_ShouldMoveToIterationStep()
        {
            testEngine.Run(@"

var arr = [0,1,2,];
loop arr
{
    print (i);
    continue;
    print (""FAIL"");
}");

            Assert.AreEqual("012", testEngine.InterpreterResult);
        }

        [Test]
        public void Loop_WhenNestedContinued_ShouldMoveToIterationStep()
        {
            testEngine.Run(@"
var arr = [0,1,2,3,4,5,6,7,8,9];
loop arr
{
    var check1 = (item % 2) == 0;
    if(check1)
    {
        continue;
    }
    var check2 = (item % 3) == 0;
    if(check2)
    {
        continue;
    }

    print (item);
}");

            Assert.AreEqual("157", testEngine.InterpreterResult);
        }

        [Test]
        public void Loop_WhenNestedLocalsAndContinued_ShouldMoveToIterationStep()
        {
            testEngine.Run(@"

class Thing
{
    var a = 1,b = 2,c = 3;

    Meth1(arg1, arg2)
    {
        this.a = arg1;
        this.b = arg2;
        this.c = arg1 - arg2;
    }

    Meth2(arg1, arg2)
    {
        this.b = arg1;
        this.c = arg2;
        this.a = arg1 - arg2;
    }
}

var obj = Thing();

var arr = [9,8,7,6,5,4,3,2,1,];
loop arr
{
    obj.Meth1(item, i);
    if(obj.c > 0)
    {
        var mix = (i + item) / -2;
        obj.Meth2(mix, item);
        continue;
    }
    obj.Meth2(item, i);
    if(obj.a > 0)
    {
        continue;
    }
}");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Loop_WhenComplexNestedLocalsAndContinued_ShouldMoveToIterationStep()
        {
            testEngine.Run(@"
class Vec2
{
	static Create(x,y)
	{
		var ret = Vec2();
		ret.x = x;
		ret.y = y;
		retval = ret;
	}
    
    static Scale(v2, scalar)
    {
        retval = Vec2.Create(v2.x* scalar, v2.y*scalar);
    }

	static Rotate(v2, rad)
	{
		var cos = Math.Cos(rad);
		var sin = Math.Sin(rad);
		var x = v2.x * cos - v2.y * sin;
		var y = v2.x * sin + v2.y * cos;
		retval = Vec2.Create(x,y);
	}

	static Lerp(a, b, p)
	{
		var x = a.x + (b.x - a.x) * p;
		var y = a.y + (b.y - a.y) * p;
		retval = Vec2.Create(x,y);
	}

	static Length(v2)
	{
		retval = Math.Sqrt(v2.x*v2.x + v2.y*v2.y);
	}

	//TODO needs tests
	static Normalise(v2)
	{
		var len = Math.Sqrt(v2.x*v2.x + v2.y*v2.y);
		retval = Vec2.Create(v2.x/len, v2.y/len);
	}

	static Dot(lhs, rhs)
	{
		retval = lhs.x * rhs.x + lhs.y * rhs.y;
	}

	static Angle(v)
	{
		var rads = Math.Atan2(v.y, v.x);
		retval = Math.Rad2Deg(rads);
	}

	var x = 0, y = 0;

	_add(lhs, rhs)
	{
		retval = Vec2.Create(lhs.x + rhs.x, lhs.y + rhs.y);
	}

	_sub(lhs, rhs)
	{
		retval = Vec2.Create(lhs.x - rhs.x, lhs.y - rhs.y);
	}

	_mul(lhs, rhs)
	{
		retval = Vec2.Create(lhs.x * rhs.x, lhs.y * rhs.y);
	}

	_eq(lhs, rhs)
	{
		retval = lhs.x == rhs.x and lhs.y == rhs.y;
	}
}

class ShipControls
{
    var throttle = 0;
    var rudder = 0;
}

class Ship
{
    var pos = Vec2();
    var vel = Vec2();
    var heading = 0;
    var controls = ShipControls();
}

class EnemyShipAIBasic
{
    Tick(enemyShips, targetShips, dt)
    {
        loop enemyShips, ship
        {
            var pos = ship.pos;
            var bestTarget = EnemyTargetSelection.Select(ship, targetShips);

            if (bestTarget)
            {
                var distanceAheadScale = 0.1;
                var headingAlignForFar = 35;
                var farDist = 15;
                var posDiff = bestTarget.pos - ship.pos;
                var velDiff = bestTarget.vel - ship.vel;
                var headingDiff = ship.heading - bestTarget.heading;
                var headingDiffAbs = Math.Abs(headingDiff);
                var approachingDot = Vec2.Dot(Vec2.Normalise(posDiff), Vec2.Normalise(ship.vel));
                var isMovingTowards = approachingDot > 0;
                var distance = Vec2.Length(posDiff);
                var isFar = distance > farDist;
                var isNear = !isFar;
                var distNorm = distance / farDist;
                
                var aheadTargetRudder =  EnemyShipAIBasic.RudderFromHeadingDiff(headingDiff, headingDiffAbs);
                var prevThrottle = ship.controls.throttle;
                var prevRudder = ship.controls.rudder;
                
                if(isMovingTowards)
                {
                    ship.controls.throttle = 1;
                    ship.controls.rudder = aheadTargetRudder;
                    continue;
                }

                if(isFar)
                {
                    ship.controls.throttle = 1;
                    if(headingDiffAbs < 45)
                        ship.controls.throttle = 0;
                    
                    continue;
                }

                if(isNear)
                {
                    ship.controls.throttle = prevThrottle;
                    ship.controls.rudder = aheadTargetRudder * distNorm;
                    continue;
                }
            }
            else
            {
                ship.controls.throttle = 1;
                ship.controls.rudder = 1;
            }
        }
    }

    RudderFromHeadingDiff(headingDiff, absHeadingDiff)
    {
        var amt = absHeadingDiff / 180;
        if(headingDiff > 0)
            retval = amt;
        else
            retval = -amt;
    }
}

class EnemyTargetSelection
{
    Select(enemyShip, availTargets)
    {
        retval = availTargets[0];
    }
}

var enemyShips = [];
for(var i = 0; i < 10; i +=1 )
{
    var ship = Ship();
    ship.pos =  Vec2.Create(i*2, 0);
    enemyShips.Add(ship);
}
var targetShips = [Ship()];

EnemyShipAIBasic.Tick(enemyShips, targetShips, 0.1);
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }
    }
}