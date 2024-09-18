namespace ULox.Core.Bench
{
    public static class BenchmarkScripts
    {
        public static readonly Script Loop = new(nameof(Loop), @"
var arr = [];
arr.Resize(100,0);

loop arr
{
    item = i;
}");

        public static readonly Script If = new(nameof(If), @"
var i = 0;

if(i == 1)
{
    i = 1;
}
else if (i == 2)
{
    i = 2;
}
else
{
    i = 3;
}
");
    }

    public static class Vec2Variants
    {
        public static readonly Script Type = new(nameof(Vec2Variants), @"
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
		if(retval != retval)
		{
			retval = 0;
		}
	}

	//TODO needs tests
	static Normalise(v2)
	{
		var len = Vec2.Length(v2);
		if(len != 0)
		{
			retval = Vec2.Create(v2.x/len, v2.y/len);
			return;
		}
		retval = Vec2.Create(0,0);
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


testset Vec2Tests
{
	test Default
	{
		var expected = 0;
		var result = Vec2();

		var x = result.x;
		var y = result.y;

		Assert.AreEqual(expected, x);
		Assert.AreEqual(expected, y);
	}

	test ([
		[1,1,1,1,2,2],
		[1,0,1,2,2,2],
		[1,-1,1,-1,2,-2],
		[1,2,3,4,4,6],
	]) Add(ax, ay, bx, by, ex, ey)
	{
		var expected = Vec2.Create(ex,ey);
		var result;
		var a = Vec2.Create(ax,ay);
		var b = Vec2.Create(bx,by);

		result = a+b;

		Assert.IsTrue(expected == result);
	}

	test ([
		[1,1,1,1,0,0],
		[1,0,1,2,0,-2],
		[1,-1,1,-1,0,0],
		[1,2,3,4,-2,-2],
	]) Sub(ax, ay, bx, by, ex, ey)
	{
		var expected = Vec2.Create(ex,ey);
		var result;
		var a = Vec2.Create(ax,ay);
		var b = Vec2.Create(bx,by);

		result = a-b;

		Assert.IsTrue(expected == result);
	}

	test Mul
	{
		var expected = Vec2.Create(3,8);
		var result;
		var a = Vec2.Create(1,2);
		var b = Vec2.Create(3,4);

		result = a*b;

		Assert.IsTrue(expected == result);
	}

	test ([
		[1,1,1,1,true],
		[1,0,1,2,false],
		[1,-1,1,-1,true],
		[1,2,3,4,false],
	]) Equal(ax, ay, bx, by, expected)
	{
		var result;
		var a = Vec2.Create(ax,ay);
		var b = Vec2.Create(bx,by);

		result = a == b;

		Assert.AreEqual(expected, result);
	}

	test Scale
	{
		var expected = Vec2.Create(2,4);
		var result;
		var a = Vec2.Create(1,2);
		var b = 2;

		result = Vec2.Scale(a,b);

		Assert.IsTrue(expected == result);
	}

	test Rotate
	{
		var expected = Vec2.Create(0,1);
		var result;
		var a = Vec2.Create(1,0);
		var b = Math.Pi()/2;

		result = Vec2.Rotate(a,b);

		var expRes = expected - result;
		var expResMag = Vec2.Length(expRes);
		expect expResMag < 0.0001;
	}

	test Lerp
	{
		var expected = Vec2.Create(1,2);
		var result;
		var a = Vec2.Create(0,0);
		var b = Vec2.Create(2,4);
		var p = 0.5;

		result = Vec2.Lerp(a,b,p);

		expect expected == result;
	}

	test ([
		[1,0,0],
		[1,1,45],
		[0,1,90],
		[-1,0,180],
		[0,-1,-90],
	]) Angle(x, y, expected)
	{
		var result;
		var a = Vec2.Create(x,y);

		result = Vec2.Angle(a);

		expect expected == result: ""expected: "" + expected + "" got: "" + result;
	}
}

fun Bench()
{
	var a = Vec2.Create(1,2);
	var b = Vec2.Create(3,5);
	a = Vec2.Scale(a, 2);
    var d = a + b;
    var l = Vec2.Length(d);
	var n = Vec2.Normalise(d);
}
");
        public static readonly Script Tuple = new(nameof(Vec2Variants), @"

class Vec2
{
	static Length(x1,y1)
	{
	    retval = Math.Sqrt(x1*x1 + y1*y1);
        if(retval != retval)
        {
            retval = 0;
        }
	}

	static Normalise(x1,y1) (xRet, yRet)
    {
        var len = Vec2.Length(x1,y1);
        if(len != 0)
        {
			xRet = x1/len;
	        yRet = y1/len;
            return;
        }

		xRet = 0;
        yRet = 0;
    }
}

fun Bench()
{
	var (x1,y1) = (1,2);
	var (x2,y2) = (3,5);
	x1 *= 2;
	y1 *= 2;
	var (x3,y3) = (x1 + x2, y1 + y2);
	var len = Vec2.Length(x3,y3);
	var (x4,y4) = (x3/len, y3/len);
}");
    }
}
