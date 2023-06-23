using ULox;

var compileRepeat = 1000;
var runRepeat = 1000;
var callFuncRepeat = 10000;
var funcSetupName = "SetupGame";
var funcUpdateName = "Update";

var targetScriptText = @"
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
	loop balls
	{
		item.Tick();
	}
}";

var targetScript = new Script("", targetScriptText);

while (true)
{
    Console.WriteLine($"Compiling only {compileRepeat} times.");
    for (int i = 0; i < compileRepeat; i++)
    {
        var engine = Engine.CreateDefault();
        var compiled = engine.Context.CompileScript(targetScript);
    }

    Console.WriteLine($"RunScript {runRepeat} times.");
    for (int i = 0; i < runRepeat; i++)
    {
        var engine = Engine.CreateDefault();
        engine.RunScript(targetScript);
    }

    Console.WriteLine($"Call {funcUpdateName} {callFuncRepeat} times.");
    {
        var engine = Engine.CreateDefault();
        engine.RunScript(targetScript);
        engine.Context.Vm.Globals.Get(new HashedString(funcSetupName), out var funcStartValue);
        engine.Context.Vm.Globals.Get(new HashedString(funcUpdateName), out var funcUpdateValue);
        engine.Context.Vm.PushCallFrameAndRun(funcStartValue, 0);
        for (int i = 0; i < callFuncRepeat; i++)
        {
            engine.Context.Vm.PushCallFrameAndRun(funcUpdateValue, 0);
        }
    }
}