using NUnit.Framework;

namespace ULox.Tests
{
    public class FunctionalCollectionTests : EngineTestBase
    {
        [Test]
        public void NativeListMap_WhenCompiled_ShouldNotError()
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

fun isEven(val)
{
    return ((val % 2) != 0);
}

var arr = MakeTestArray();
var reducedResult = arr.Reduce(accum);
Assert.AreEqual(10, reducedResult);

arr = MakeTestArray();
arr = arr.Map(addone);
var mapReducedResult = arr.Reduce(accum);
Assert.AreEqual(15, mapReducedResult);

arr = MakeTestArray();
var foldRes = arr.Fold(accum, 1);
Assert.AreEqual(11, foldRes);

arr = MakeTestArray();
arr = arr.Filter(isEven);
var foldRes = arr.Reduce(accum);
Assert.AreEqual(4, foldRes);
";

            testEngine.MyEngine.Context.AddLibrary(new AssertLibrary(() => new Vm()));
            testEngine.Run(script);

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void FunctionalCollectionUlox_WhenCompiled_ShouldNotError()
        {
            var script = @"
fun reduce(arr, fn)
{
    var res = arr[0];
    var len = arr.Count();
    for(var i = 1; i < len; i += 1)
    {
        res = fn(arr[i], res);
    }
    return res;
}

fun fold(arr, fn, initVal)
{
    var res = initVal;
    var len = arr.Count();
    for(var i = 0; i < len; i += 1)
    {
        res = fn(arr[i], res);
    }
    return res;
}

fun map(arr, fn)
{
    var res = [];
    var len = arr.Count();
    res.Resize(len, null);
    for(var i = 0; i < len; i += 1)
    {
        res[i] = fn(arr[i]);
    }
    return res;
}

fun filter(arr, fn)
{
    var res = [];
    var len = arr.Count();
    for(var i = 0; i < len; i += 1)
    {
        var val = arr[i];
        if(fn(val))
            res.Add(val);
    }
    return res;
}

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


test FunctionalCollectionTests
{
    testcase FoldSum
    {
        var arr = MakeTestArray();

        var result = fold(arr, accum, 0);

        Assert.AreEqual(10, result);
    }

    testcase ReduceSum
    {
        var arr = MakeTestArray();

        var result = reduce(arr, accum);

        Assert.AreEqual(10, result);
    }

    testcase MapAddOne
    {
        var arr = MakeTestArray();
        fun addone(val)
        {
            return val + 1;
        }

        var result = map(arr, addone);
        
        var reducedResult = reduce(result, accum);
        Assert.AreEqual(15, reducedResult);
    }

    testcase FilterIsEven
    {
        var arr = MakeTestArray();
        fun isEven(val)
        {
            return ((val % 2) != 0);
        }

        var result = filter(arr, isEven);
        
        var foldResult = fold(result, accum, 0);
        Assert.AreEqual(4, foldResult);
    }
}
";

            testEngine.MyEngine.Context.AddLibrary(new AssertLibrary(() => new Vm()));
            testEngine.Run(script);

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

    }
}