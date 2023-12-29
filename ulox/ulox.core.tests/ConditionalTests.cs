using NUnit.Framework;

namespace ULox.Core.Tests
{
    public class ConditionalTests : EngineTestBase
    {
        [Test]
        public void If_WhenFalseSingleStatementBody_ShouldSkipAfter()
        {
            testEngine.Run(@"
if(1 > 2) 
    print (""ERROR""); 

print (""End"");");

            Assert.AreEqual("End", testEngine.InterpreterResult);
        }

        [Test]
        public void If_WhenFalseAndElse_ShouldHitElse()
        {
            testEngine.Run(@"
if(1 > 2)
    print (""ERROR"");
else
    print (""The "");

print (""End"");");

            Assert.AreEqual("The End", testEngine.InterpreterResult);
        }

        [Test]
        public void If_WhenCompoundLogicExpressions_ShouldHitFalse()
        {
            testEngine.Run(@"
if(1 > 2 or 2 > 3)
    print( ""ERROR"");
else if (1 == 1 and 2 == 2)
    print (""The "");

print (""End"");");

            Assert.AreEqual("The End", testEngine.InterpreterResult);
        }

        [Test]
        public void If_WhenNested_ShouldRunInnerElse()
        {
            testEngine.Run(@"
var heading = 330;

//why does the following not work
if(heading < 350)
{
    if(heading < 180)
    {
    }
    print(""inner after"");
}
");

            Assert.AreEqual("inner after", testEngine.InterpreterResult);
        }

        [Test]
        public void If_WhenNestedWithElse_ShouldRunInnerElse()
        {
            testEngine.Run(@"
var heading = 330;

//why does the following not work
if(heading < 350)
{
    if(heading < 180)
    {
    }
else
{}
    print(""inner after"");
}
else
{}
");

            Assert.AreEqual("inner after", testEngine.InterpreterResult);
        }

        [Test]
        public void If_WhenNestedFor_ShouldRunInner()
        {
            testEngine.Run(@"
var res = [];
for(var i = 0; i < 5; i += 1)
{
    if(i)
    {
        res.Add(i);
    }
}

print(res.Count());
");

            Assert.AreEqual("5", testEngine.InterpreterResult);
        }

        [Test]
        public void If_WhenNestedForMany_ShouldRunInner()
        {
            testEngine.Run(@"
var len = 500;
var res = [];
for(var i = 0; i < len; i += 1)
{
    if(i)
    {
        res.Add(i);
    }
}

print(res.Count());
");

            Assert.AreEqual("500", testEngine.InterpreterResult);
        }

        [Test]
        public void If_WhenNestedForNegated_ShouldRunInner()
        {
            testEngine.Run(@"
var len = 5;
var res = [];
for(var i = 0; i < len; i += 1)
{
    if(!i)
    {
    }
    else
    {
        res.Add(i);
    }
}

print(res.Count());
");

            Assert.AreEqual("5", testEngine.InterpreterResult);
        }

        [Test]
        public void If_WhenNestedLoop_ShouldRunInner()
        {
            testEngine.Run(@"
fun GetArray()
{
    retval = [0,1,2,3,4,];
}

var arr = GetArray();
var len = arr.Count();
var res = [];
loop arr
{
    if(item)
    {
        res.Add(item);
    }
}

print(res.Count());
");

            Assert.AreEqual("5", testEngine.InterpreterResult);
        }

        [Test]
        public void If_WhenNestedUnrolled_ShouldRunInner()
        {
            testEngine.Run(@"
fun isEven(val)
{
    retval = ((val % 2) == 0);
}

fun GetArray()
{
    retval = [0,1,2,3,4,];
}

var arr = GetArray();
var len = arr.Count();
var res = [];
{
    var i = 0;
    var val = arr[i];
    if(isEven(val))
        res.Add(val);
    i+=1;
    val = arr[i];
    if(isEven(val))
        res.Add(val);
    i+=1;
    val = arr[i];
    if(isEven(val))
        res.Add(val);
    i+=1;
    val = arr[i];
    if(isEven(val))
        res.Add(val);
    i+=1;
    val = arr[i];
    if(isEven(val))
        res.Add(val);
    i+=1;
}

print(res.Count());
");

            Assert.AreEqual("3", testEngine.InterpreterResult);
        }

        [Test]
        public void If_WhenExpression_ShouldRun()
        {
            testEngine.Run(@"
fun isEven(val)
{
    retval = ((val % 2) == 0);
}

var val = 1;

if(isEven(val))
    print(val);

val = 2;

if(isEven(val))
    print(val);
");

            Assert.AreEqual("2", testEngine.InterpreterResult);
        }
    }
}
