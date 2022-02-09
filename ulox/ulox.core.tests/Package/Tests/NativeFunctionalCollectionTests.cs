using NUnit.Framework;

namespace ULox.Tests
{
    public class NativeFunctionalCollectionTests : EngineTestBase
    {
        [Test]
        public void Reduce_WhenGiven5intsAndAccum_ShouldReturn10()
        {
            var script = @"
fun MakeTestArray()
{
    var arr = [];

    for(var i = 0;i < 5; i += 1)
    {
        arr.Add(i);
    }

    return arr;
}

fun accum(cur, running)
{
    return running + cur;
}

var arr = MakeTestArray();
var reducedResult = arr.Reduce(accum);
print(reducedResult);
";

            testEngine.Run(script);

            Assert.AreEqual("10", testEngine.InterpreterResult);
        }

        [Test]
        public void Fold_WhenGiven1And5intsAndAccum_ShouldReturn11()
        {
            var script = @"
fun MakeTestArray()
{
    var arr = [];

    for(var i = 0;i < 5; i += 1)
    {
        arr.Add(i);
    }

    return arr;
}

fun accum(cur, running)
{
    return running + cur;
}

var arr = MakeTestArray();
var foldRes = arr.Fold(accum, 1);
print(foldRes);
";

            testEngine.Run(script);

            Assert.AreEqual("11", testEngine.InterpreterResult);
        }

        [Test]
        public void Map_WhenGiven5intsAndAddOneAndReduceAccum_ShouldReturn15()
        {
            var script = @"
fun MakeTestArray()
{
    var arr = [];

    for(var i = 0;i < 5; i += 1)
    {
        arr.Add(i);
    }

    return arr;
}

fun accum(cur, running)
{
    return running + cur;
}

fun addone(val)
{
    return val + 1;
}

var arr = MakeTestArray();
arr = arr.Map(addone);
var mapReducedResult = arr.Reduce(accum);
print(mapReducedResult);
";

            testEngine.Run(script);

            Assert.AreEqual("15", testEngine.InterpreterResult);
        }

        [Test]
        public void Filter_WhenGiven5intsIsEvenAndReduceAccum_ShouldReturn4()
        {
            var script = @"
fun MakeTestArray()
{
    var arr = [];

    for(var i = 0;i < 5; i += 1)
    {
        arr.Add(i);
    }

    return arr;
}

fun accum(cur, running)
{
    return running + cur;
}

fun isEven(val)
{
    return ((val % 2) != 0);
}

var arr = MakeTestArray();
arr = arr.Filter(isEven);
var filterReduceRes = arr.Reduce(accum);
print(filterReduceRes);
";

            testEngine.Run(script);

            Assert.AreEqual("4", testEngine.InterpreterResult);
        }

        [Test]
        public void OrderBy_WhenGiven5OutOfOrderInts_ShouldReturnInOrder()
        {
            var script = @"
fun MakeTestArray()
{
    var arr = [];

    for(var i = 0;i < 5; i += 1)
    {
        arr.Add(5-i);
    }

    return arr;
}

fun self(x) { return x; }

fun prnt(x) {print(x);}

var arr = MakeTestArray();
arr = arr.OrderBy(self);
arr.Map(prnt);
";

            testEngine.Run(script);

            Assert.AreEqual("12345", testEngine.InterpreterResult);
        }

        [Test]
        public void First_WhenGiven5IntsAndIs3_ShouldReturn3()
        {
            var script = @"
fun MakeTestArray()
{
    var arr = [];

    for(var i = 0;i < 5; i += 1)
    {
        arr.Add(i);
    }

    return arr;
}

fun isthree(x) { return x == 3; }

var arr = MakeTestArray();
var res = arr.First(isthree);
print(res);

fun isseven(x) { return x == 7; }

var res = arr.First(isseven);
print(res);
";

            testEngine.Run(script);

            Assert.AreEqual("3null", testEngine.InterpreterResult);
        }

        [Test]
        public void Fork_WhenGiven3Funcs_ShouldRunEachInOrderAndReturn3CountArray()
        {
            var script = @"
fun One(x){print(1 + x); return true;}
fun Two(x){print(2 + x); return true;}
fun Three(x){print(3 + x); return true;}
var arr = [];
arr.Add(One);
arr.Add(Two);
arr.Add(Three);

var runOn = "" Count"";

fun andChainer(x, running) {return (x and running);}

var forkRes = arr.Fork(runOn);
var forkReduceRes = forkRes.Reduce(andChainer);
print(forkReduceRes);
";

            testEngine.Run(script);

            Assert.AreEqual("1 Count2 Count3 CountTrue", testEngine.InterpreterResult);
        }

        [Test]
        public void Until_WhenGiven3FuncsSecondSuccess_ShouldRunFirstTwoOnly()
        {
            var script = @"
fun One(x){print(1 + x); return false;}
fun Two(x){print(2 + x); return true;}
fun Three(x){print(3 + x); return true;}
var arr = [];
arr.Add(One);
arr.Add(Two);
arr.Add(Three);

var runOn = "" Count"";

var forkRes = arr.Until(runOn);
";

            testEngine.Run(script);

            Assert.AreEqual("1 Count2 Count", testEngine.InterpreterResult);
        }
    }
}