using NUnit.Framework;
using ULox;

namespace ulox.core.tests
{
    [TestFixture]
    public class ProfileSourceTests : EngineTestBase
    {
        [Test]
        public void Profile()
        {
            testEngine.Run(@"
var dt = 0;
var limit = 5;
var numBallsToSpawn = 200;

var ballName = ""BouncyBall"";
var balls = [];

fun RandVec2()
{
	return (-3,3);
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

	Tick()
	{
  		this.posx = this.posx + this.velx * dt;
  		this.posy = this.posy + this.vely * dt;

		this.Contain(limit);
		//this.UpdateGoPosition();
	}
	
	Contain(lim)
	{
		var x = this.posx;
		var y = this.posy;
		var vx = this.velx;
		var vy = this.vely;

		if((x < -lim and vx < 0) or 
		  (x > lim  and vx > 0) )
		{
			vx *= -1;
			this.velx = vx;
		}

		if((y < -lim and vy < 0) or
		  (y > lim  and vy > 0) )
		{
			vy *= -1;
			this.vely = vy;
		}
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
	loop(balls)
	{
		item.Tick();
	}
}");

            testEngine.MyEngine.Context.Vm.PushCallFrameAndRun(testEngine.MyEngine.Context.Vm.GetGlobal(new HashedString("SetupGame")), 0);
            testEngine.MyEngine.Context.Vm.PushCallFrameAndRun(testEngine.MyEngine.Context.Vm.GetGlobal(new HashedString("Update")), 0);

            Assert.AreEqual("Setting Up GameUpdating", testEngine.InterpreterResult);
        }
    }
}
