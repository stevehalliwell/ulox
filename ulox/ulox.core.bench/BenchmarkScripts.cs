namespace ULox.Core.Bench
{
    public static class BenchmarkScripts
    {
        public static readonly Script While = new Script(nameof(While), @"
var i = 0;
var arr = [];
arr.Resize(100,0);

while(i < 100)
{
    arr[i] = i;
    i+=1;
}");

        public static readonly Script For = new Script(nameof(For), @"
var arr = [];
arr.Resize(100,0);

for(var i = 0; i < 100; i+=1)
{
    arr[i] = i;
}");

        public static readonly Script Loop = new Script(nameof(Loop), @"
var arr = [];
arr.Resize(100,0);

loop arr
{
    item = i;
}");

        public static readonly Script If = new Script(nameof(If), @"
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

        public static readonly Script Match = new Script(nameof(Match), @"
var i = 0;
match i 
{
1: i = 1;
2: i = 2;
0: i = 3;
}");
    }

    public static class BouncingBallProfileScript
    {
        public static readonly Script Script = new Script(nameof(BouncingBallProfileScript), @"
var dt = 0;
var limit = 5;
var numBallsToSpawn = 200;

var balls = [];

fun RandVec2() (x,y)
{
	x=-3;
	y=3;
}

class Ball
{
	init()
	{
		var (x,y) = RandVec2();
		this.posx = x;
		this.posy = y;

		var (vx,vy) = RandVec2();
		this.velx = vx;
		this.vely = vy;
	}
}

fun SetupGame()
{
	print (""Setting Up Game"");

	for(var i = 0; i < numBallsToSpawn; i += 1)
	{
		balls.Add(Ball());
	}
	print(balls.Count());
}

fun Update()
{
	var lim = limit;

	var iter = 0;

	loop balls
	{
		//print(GenerateStackDump());
  		item.posx = item.posx + item.velx * dt;
  		item.posy = item.posy + item.vely * dt;

		var x = item.posx;
		var y = item.posy;
		var vx = item.velx;
		var vy = item.vely;

		if((x < -lim and vx < 0) or 
		  (x > lim  and vx > 0) )
		{
			vx *= -1;
			item.velx = vx;
		}

		if((y < -lim and vy < 0) or
		  (y > lim  and vy > 0) )
		{
			vy *= -1;
			item.vely = vy;
		}
	}
	print (""Updated"");
}
");
    }

    public static class WaterLineProfileScript
    {
        public static readonly Script Script = new Script(nameof(WaterLineProfileScript), @"
var dt = 0;
var points = [];

data Circle
{
	x = 0,
	y = 0, 
	py = 0,
	anchorY = 0,
	pullY = 0,
}

fun SetupGame()
{
	print (""Setting Up Game"");

	var numToSpawn = 50;
	var startX = -12.5;
	var stepX = 0.5;

	for(var i = 0; i < numToSpawn; i += 1)
	{
		var circ = Circle();
		circ.x = startX + i * stepX;
		points.Add(circ);
	}
}

fun Update()
{
	var pullFactor = 2;
	var firstNeighScale = 0.75;
	var secondNeighScale = 0.65;
	var thirdNeighScale = 0.35;
	var pDevScale = 0.5;
	var dragSharpness = 3;
	var dragFac = Math.Exp(-dragSharpness * dt) * dt;

	var circCount = points.Count();
	for(var i = 0; i < circCount; i += 1)
	{
		var item = points[i];
		var px = 0;
		var py = 0;
		var y = item.y;
		var neigh;

		neigh = points[(i-3 + circCount) % circCount];
		py += (neigh.y - y) * thirdNeighScale;
		neigh = points[(i-2 + circCount) % circCount];
		py += (neigh.y - y) * secondNeighScale;
		neigh = points[(i-1 + circCount) % circCount];
		py += (neigh.y - y) * firstNeighScale;
		neigh = points[(i+1) % circCount];
		py += (neigh.y - y) * firstNeighScale;
		neigh = points[(i+2)% circCount];
		py += (neigh.y - y) * secondNeighScale;
		neigh = points[(i+3)% circCount];
		py += (neigh.y - y) * thirdNeighScale;

		item.pullY = py;
	}

	loop points
	{
		var dy = item.y - item.py;
		item.py = item.y;
		var pull = item.pullY;
		item.pullY = 0;
		var y = item.y;
		var pDev = y - item.anchorY;
		
		pull += -pDev * pDevScale;
		pull *= pullFactor;
		pull *= dt;
		y += dy + pull;
		var vel = (y - item.py) / dt;
		y = item.py + vel * dragFac;
		item.y = y;
	}
	print (""Updated"");
}");
    }

	public static class Vec2Variants
	{
		public static readonly Script Type = new Script(nameof(Vec2Variants), @"
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
		public static readonly Script Tuple = new Script(nameof(Vec2Variants), @"

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
