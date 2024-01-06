using NUnit.Framework;

namespace ULox.Core.Tests
{
    public class FunctionalCollectionTests : EngineTestBase
    {
        [Test]
        public void NativeListMap_WhenCompiled_ShouldNotError()
        {
            testEngine.Run(@"
fun MakeTestArray()
{
    var arr = [];

    for(var i = 0;i < 5; i += 1)
    {
        arr.Add(i);
    }

    retval = arr;
}

fun accum(cur, running)
{
    retval = running + cur;
}

fun addone(val)
{
    retval = val + 1;
}

fun isEven(val)
{
    retval = ((val % 2) != 0);
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
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void FunctionalCollectionUlox_WhenCompiled_ShouldNotError()
        {
            testEngine.Run(@"
fun reduce(arr, fn)
{
    var res = arr[0];
    loop arr
    {
        res = fn(item, res);
    }
    retval = res;
}

fun fold(arr, fn, initVal)
{
    var res = initVal;
    loop arr
    {
        res = fn(item, res);
    }
    retval = res;
}

fun map(arr, fn)
{
    var res = [];
    var len = arr.Count();
    res.Resize(len, null);
    loop arr
    {
        res[i] = fn(item);
    }
    retval = res;
}

fun filter(arr, fn)
{
    var res = [];
    var len = arr.Count();
    loop arr
    {
        if(fn(item))
            res.Add(item);
    }
    retval = res;
}

fun accum(cur, running)
{
    retval = running + cur;
}


testset FunctionalCollectionTests
{
    test FoldSum
    {
        var arr = [0,1,2,3,4,];

        var result = fold(arr, accum, 0);

        Assert.AreEqual(10, result);
    }

    test ReduceSum
    {
        var arr = [0,1,2,3,4,];

        var result = reduce(arr, accum);

        Assert.AreEqual(10, result);
    }

    test MapAddOne
    {
        var arr = [0,1,2,3,4,];
        fun addone(val)
        {
            retval = val + 1;
        }

        var result = map(arr, addone);
        
        var reducedResult = reduce(result, accum);
        Assert.AreEqual(15, reducedResult);
    }
}");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }
    }
}