using NUnit.Framework;

namespace ULox.Core.Tests
{
    [TestFixture]
    public class MutliReturnTests : EngineTestBase
    {
        [Test]
        public void Vec2Add_WhenGivenKnowValues_ShouldReturnExpected()
        {
            testEngine.Run(@"
var a = 1,b = 2,c = 3,d = 4;

fun Add(x1,y1, x2, y2) (x,y) 
{
    x = x1 + x2;
    y = y1 + y2;
}

var (x,y) = Add(a,b,c,d);

print(x);
print(y);");

            Assert.AreEqual("46", testEngine.InterpreterResult);
        }

        [Test]
        public void Run_WhenMultiVarAssignInline_ShouldMatchExpected()
        {
            testEngine.Run(@"
var (a,b) = (1,2);

print(a);
print(b);");

            Assert.AreEqual("12", testEngine.InterpreterResult);
        }

        [Test]
        public void Run_WhenMultiVarAssignInlineNested_ShouldMatchExpected()
        {
            testEngine.Run(@"
{
	var (a,b,c) = (1,2,3);

	print(a);
	print(b);
	print(c);
}");

            Assert.AreEqual("123", testEngine.InterpreterResult);
        }

        [Test]
        public void Vec2Add_WhenGivenKnowValuesNested_ShouldReturnExpected()
        {
            testEngine.Run(@"
var a = 1,b = 2,c = 3,d = 4;

fun Add(x1,y1, x2, y2) (x,y) 
{
    x = x1 + x2;
    y = y1 + y2;
}

{
	var (x,y) = Add(a,b,c,d);

	print(x);
	print(y);
}");

            Assert.AreEqual("46", testEngine.InterpreterResult);
        }


        [Test]
        public void Run_WhenReturn2AndTake2_ShouldMatchExpected()
        {
            testEngine.Run(@"
fun A()(a,b){a=1;b=2;}

var (res1,res2) = A();

print(res1);
print(res2);");

            Assert.AreEqual("12", testEngine.InterpreterResult);
        }

        [Test]
        public void Run_WhenReturn2AndTake2WithParams_ShouldMatchExpected()
        {
            testEngine.Run(@"
fun A(a,b)(retA, retB){retA = a+1; retB = b+2;}

var (res1,res2) = A(1,2);

print(res1);
print(res2);");

            Assert.AreEqual("24", testEngine.InterpreterResult);
        }

        [Test]
        public void Run_WhenReturn2Take1_ShouldError()
        {
            testEngine.Run(@"
fun A()(a,b){a=1;b=2;}

var (res1) = A(); //2 is left on stack

print (res1);");

            StringAssert.StartsWith("Multi var assign to result mismatch. Taking '1' but results contains '2' at ip:'8' in chunk:'unnamed_chunk(test:4)'.", testEngine.InterpreterResult);
        }

        [Test]
        public void Run_WhenReturn2Take3_ShouldError()
        {
            testEngine.Run(@"
fun A()(a,b){a=1;b=2;}

var (res1,res2,res3) = A();

print (res1);
print (res2);
print (res3);");

            StringAssert.StartsWith("Multi var assign to result mismatch. Taking '3' but results contains '2' at ip:'8' in chunk:'unnamed_chunk(test:4)'.", testEngine.InterpreterResult);
        }

        [Test]
        public void Run_WhenReturn5AndTake5_ShouldMatchExpected()
        {
            testEngine.Run(@"
fun A() (a,b,c,d,e) {a=1;b=2;c=3;d=4;e=5;}

var (a,b,c,d,e) = A();

print(a);
print(b);
print(c);
print(d);
print(e);");

            Assert.AreEqual("12345", testEngine.InterpreterResult);
        }

        [Test]
        public void Run_WhenReturnNoneTake2_ShouldError()
        {
            testEngine.Run(@"
fun A(){return;}

var (res1,res2) = A();

print(res1);
print(res2);");

            StringAssert.StartsWith("Multi var assign to result mismatch. Taking '2' but results contains '1' at ip:'8' in chunk:'unnamed_chunk(test:4)'.", testEngine.InterpreterResult);
        }

        [Test]
        public void Run_WhenVec2Tuple_ShouldNotError()
        {
            testEngine.Run(@"
class Vec2
{
	static Rotate(xIn, yIn, rad) (xOut, yOut)
	{
		var cos = Math.Cos(rad);
		var sin = Math.Sin(rad);
		xOut = xIn * cos - yIn * sin;
		yOut = xIn * sin + yIn * cos;
	}

	static Lerp(x1,y1,x2,y2,p) (xOut, yOut)
	{
		xOut = x1 + (x2 - x1) * p;
		yOut = y1 + (y2 - y1) * p;
	}

	static Length(x,y)
	{
		retval = Math.Sqrt(x*x + y*y);
		if(retval != retval)
		{
			retval = 0;
		}
	}

	//TODO needs tests
	static Normalise(xIn, yIn) (xOut, yOut)
	{
		var len = Vec2.Length(xIn, yIn);
		if(len != 0)
		{
			xOut = xIn / len;
			yOut = yIn / len;
			return;
		}
		xOut = 0;
		yOut = 0;
	}

	//TODO needs tests
	static Dot(x1, y1, x2, y2)
	{
		retval = x1 * x2 + y1 * y2;
	}

	static Angle(x, y)
	{
		var rads = Math.Atan2(y, x);
		retval = Math.Rad2Deg(rads);
	}
}

testset Vec2Tests
{
	test Rotate
	{
		var (expectedX, expectedY) = (0,1);
		var (ax, ay) = (1,0);
		var b = Math.Pi()/2;

		var (resX, resY) = Vec2.Rotate(ax,ay,b);

		expect 
			Math.Abs(expectedX - resX) < 0.001,
			Math.Abs(expectedY - resY) < 0.001;
	}

	test Lerp
	{
		var (expectedX, expectedY) = (1,2);
		var (ax, ay) = (0,0);
		var (bx, by) = (2,4);
		var p = 0.5;

		var (resX, resY) = Vec2.Lerp(ax,ay,bx,by,p);

		Assert.AreApproxEqual(expectedX,resX);
		Assert.AreApproxEqual(expectedY,resY);
	}

	test ([
		[1,0,0],
		[1,1,45],
		[0,1,90],
		[-1,0,180],
		[0,-1,-90],
	]) Angle(x, y, expected)
	{
		var result = Vec2.Angle(x,y);

		expect expected == result: ""expected: "" + expected + "" got: "" + result;
	}
}
");
            Assert.AreEqual("", testEngine.InterpreterResult);
        }
    }
}
