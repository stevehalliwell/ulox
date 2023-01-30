using ULox;

namespace ulox.core.bench
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

loop (arr)
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

var ballName = ""BouncyBall"";
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
		
		//this.go = CreateFromPrefab(ballName);
	}
}

fun SetupGame()
{
	print (""Setting Up Game"");

	for(var i = 0; i < numBallsToSpawn; i += 1)
	{
		balls.Add(Ball());
	}
}

fun Update()
{
	print (""Updating"");
	var lim = limit;

	loop(balls)
	{
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
}
");
    }
}
