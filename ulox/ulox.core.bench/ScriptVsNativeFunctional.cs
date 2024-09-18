namespace ULox.Core.Bench
{
    public class ScriptVsNativeFunctional
    {
        public const string CommonScript = @"

fun MakeTestArray()
{
    retval = [0,1,2,3,4,];
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

fun isthree(x) 
{ 
    retval = x == 3; 
}

fun ByOne(x){retval = x*1;}
fun ByTwo(x){retval = x*2;}
fun ByThree(x){retval = x*3;}
";

        public static readonly Script FunctionalNative = new(nameof(FunctionalNative),CommonScript + @"
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

var arr = MakeTestArray();
var firstRes = arr.First(isthree);
Assert.AreEqual(3, firstRes);

var forkMethods = [];
forkMethods.Add(ByOne);
forkMethods.Add(ByTwo);
forkMethods.Add(ByThree);
var forkOn = 3;
var forkRes = forkMethods.Fork(forkOn);
var forkReduceRes = forkRes.Reduce(accum);
Assert.AreEqual(3+6+9, forkReduceRes);
");

        public static readonly Script FunctionalUlox = new(nameof(FunctionalUlox),CommonScript + @"
fun reduce(arr, fn)
{
    var res = arr[0];
    var len = countof arr;
    for(var i = 1; i < len; i += 1)
    {
        res = fn(arr[i], res);
    }
    retval = res;
}

fun fold(arr, fn, initVal)
{
    var res = initVal;
    loop arr
    {
        res = fn(arr[i], res);
    }
    retval = res;
}

fun map(arr, fn)
{
    var res = [];
    var len = countof arr;
    res.Resize(len, null);
    loop arr
    {
        res[i] = fn(arr[i]);
    }
    retval = res;
}

fun filter(arr, fn)
{
    var res = [];
    loop arr
    {
        var val = arr[i];
        if(fn(val))
            res.Add(val);
    }
    retval = res;
}

fun filter(arr, fn)
{
    var res = [];
    loop arr
    {
        var val = arr[i];
        if(fn(val))
            res.Add(val);
    }
    retval = res;
}

fun first(arr, fn)
{
    loop arr
    {
        if(fn(item))
        {            
            retval = item;
            return;
        }
    }
    return;
}

fun fork(arr, runOn)
{
    var res = [];
    loop arr
    {
        var fn = arr[i];
        res.Add(fn(runOn));
    }
    retval = res;
}

var arr = MakeTestArray();
var reducedResult = reduce(arr, accum);
Assert.AreEqual(10, reducedResult);

arr = MakeTestArray();
var mapRes = map(arr, addone);        
var mapReducedResult = reduce(mapRes, accum);
Assert.AreEqual(15, mapReducedResult);

arr = MakeTestArray();
var foldRes = fold(arr, accum, 1);
Assert.AreEqual(11, foldRes);

arr = MakeTestArray();
var filterRes = filter(arr, isEven);
var reducedFilterRes = reduce(filterRes, accum);
Assert.AreEqual(4, reducedFilterRes);

var arr = MakeTestArray();
var firstRes = first(arr, isthree);
Assert.AreEqual(3, firstRes);

var forkMethods = [];
forkMethods.Add(ByOne);
forkMethods.Add(ByTwo);
forkMethods.Add(ByThree);
var forkOn = 3;
var forkRes = fork(forkMethods, forkOn);
var forkReduceRes = reduce(forkRes, accum);
Assert.AreEqual(3+6+9, forkReduceRes);
");
    }
}
