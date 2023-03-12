using NUnit.Framework;
using System.Linq;

namespace ULox.Core.Tests
{
    [TestFixture]
    public class OptimiserTests : EngineTestBase
    {
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            testEngine.MyEngine.Context.Program.Optimiser.Enabled = true;
        }
        
        [Test]
        public void Optimiser_NothingToOptimise_DoesNothing()
        {
            testEngine.Run("print (1+2);");

            Assert.AreEqual("3", testEngine.InterpreterResult);
            Assert.AreEqual(7, testEngine.MyEngine.Context.Program.CompiledScripts[0].TopLevelChunk.Instructions.Count);
        }

        [Test]
        public void Optimiser_UnusedLabel_RemovesDeadCode()
        {
            testEngine.Run(@"
label unused;
print (1+2);");

            Assert.AreEqual("3", testEngine.InterpreterResult);
            Assert.AreEqual(7, testEngine.MyEngine.Context.Program.CompiledScripts[0].TopLevelChunk.Instructions.Count);
        }

        [Test]
        public void Optimiser_UnusedLabelNotAtStart_RemovesDeadCode()
        {
            testEngine.Run(@"
print (1+2);
label unused;
print (1+2);");

            Assert.AreEqual("33", testEngine.InterpreterResult);
            Assert.AreEqual(13, testEngine.MyEngine.Context.Program.CompiledScripts[0].TopLevelChunk.Instructions.Count);
        }

        [Test]
        public void Optimiser_JumpOps_RemovesUnreachable()
        {
            testEngine.Run(@"
goto skip;
print(1);
goto skip;
label skip;
print(1+2);");

            Assert.AreEqual("3", testEngine.InterpreterResult);
            Assert.AreEqual(7, testEngine.MyEngine.Context.Program.CompiledScripts[0].TopLevelChunk.Instructions.Count);
        }

        [Test]
        public void Optimiser_GotoLabelWithDeadInBetween_RemovesGotoAndLabel()
        {
            testEngine.Run(@"
goto skip;
print(1);
label skip;
print(1+2);");

            Assert.AreEqual("3", testEngine.InterpreterResult);
            Assert.AreEqual(7, testEngine.MyEngine.Context.Program.CompiledScripts[0].TopLevelChunk.Instructions.Count);
        }

        [Test]
        public void Optimiser_JumpNowhere_RemovesUselessJump()
        {
            testEngine.Run(@"
goto skip;
label skip;
print(1+2);");

            Assert.AreEqual("3", testEngine.InterpreterResult);
            Assert.AreEqual(7, testEngine.MyEngine.Context.Program.CompiledScripts[0].TopLevelChunk.Instructions.Count);
        }
        
        [Test]
        [Ignore("Not yet expected to work")]
        public void Optimiser_Pong_Smaller()
        {
            var unoptCounts = new (string name, int count)[]
            {
                ("Create", 25),
("Scale", 24),
("_add", 28),
("_sub", 28),
("_mul", 28),
("_eq", 28),
("unnamed_chunk", 1248),
("FromPrefab", 14),
("Sync", 49),
("Sync", 53),
("init", 46),
("init", 18),
("init", 18),
("Update", 91),
("unnamed_chunk", 344),
("SetupGame", 8),
("CreateLevel", 37),
("CreateWalls", 117),
("CreateBall", 14),
("CreatePaddles", 48),
("Update", 10),
("UpdateGame", 36),
("unnamed_chunk", 51),
            };

            const string Vec2_ulox = @"class Vec2
{
	static Create(x,y)
	{
		var ret = Vec2();
		ret.x = x;
		ret.y = y;
		return ret;
	}
    
    static Scale(v2, scalar)
    {
        return Vec2.Create(v2.x* scalar, v2.y*scalar);
    }

	var x = 0, y = 0;

	_add(lhs, rhs)
	{
		return Vec2.Create(lhs.x + rhs.x, lhs.y + rhs.y);
	}

	_sub(lhs, rhs)
	{
		return Vec2.Create(lhs.x - rhs.x, lhs.y - rhs.y);
	}

	_mul(lhs, rhs)
	{
		return Vec2.Create(lhs.x * rhs.x, lhs.y * rhs.y);
	}

	_eq(lhs, rhs)
	{
		return lhs.x == rhs.x and lhs.y == rhs.y;
	}
}

test Vec2Tests
{
	testcase Default
	{
		var expected = 0;
		var result = Vec2();

		var x = result.x;
		var y = result.y;

		Assert.AreEqual(expected, x);
		Assert.AreEqual(expected, y);
	}

	testcase ([
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

	testcase ([
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

	testcase Mul
	{
		var expected = Vec2.Create(3,8);
		var result;
		var a = Vec2.Create(1,2);
		var b = Vec2.Create(3,4);

		result = a*b;

		Assert.IsTrue(expected == result);
	}

	testcase ([
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

	testcase Scale
	{
		var expected = Vec2.Create(2,4);
		var result;
		var a = Vec2.Create(1,2);
		var b = 2;

		result = Vec2.Scale(a,b);

		Assert.IsTrue(expected == result);
	}
}";
            const string Pong_mixins_ulox = @"class Position
{
    var pos = Vec2();
}

class Scale
{
    var scale = Vec2.Create(1,1);
}

class GameObject
{
    mixin Position,
        Scale;

    var go;

    FromPrefab(name)
    {
        this.go = CreateFromPrefab(name);
    }

    Sync()
    {
        SetGameObjectPosition(this.go, this.pos.x, this.pos.y, 0);
        SetGameObjectScale(this.go, this.scale.x, this.scale.y,1);
    }
}

class Dynamic
{
    mixin Position;
    
    var rb;
    var vel = Vec2();

    Sync()
    {
        //TODO would prefer to be able to do this during init chain 
        if(this.rb == null)
            this.rb = GetRigidBody2DFromGameObject(this.go);    
        
        SetRigidBody2DVelocity(this.rb, this.vel.x, this.vel.y);
    }
}

class PongBall
{
    mixin GameObject,
        Dynamic;

    init(pos, vel)
    {
        this.FromPrefab(""Ball"");
        this.Sync();
        SetSpriteColour(this.go, 1,0,0,1);
        SetGameObjectTag(this.go, ""Ball"");
    }
}

class PongWall
{
    mixin GameObject;
    
    init(pos, scale)
    {
        this.FromPrefab(""Wall"");
        this.Sync();
    }
}

class PongPaddle
{
    mixin GameObject,
        Dynamic;
    
    var speed;
    var upKey, downKey;
    
    init(pos, speed, upKey, downKey)
    {
        this.FromPrefab(""VerticalPaddle"");
        this.Sync();
    }

    Update()
    {
        var curSpeed = 0;
        
        if(GetKey(this.upKey))
            curSpeed = this.speed;
        if(GetKey(this.downKey))
            curSpeed = -this.speed;

        this.vel = Vec2.Create(0,curSpeed);

        SetRigidBody2DVelocity(this.rb, this.vel.x, this.vel.y);
    }
}";
            const string Pong_ulox = @"var dt;
var walls = [];
var ball;
var leftPaddle;
var rightPaddle;
var paddleMoveSpeed = 50;

fun SetupGame()
{
    CreateLevel();
}

fun CreateLevel()
{
    CreateWalls();
    
    CreateBall(Vec2.Create(0,-20), Vec2.Create(15,18));

    CreatePaddles();
}

fun CreateWalls()
{
    walls.Add(PongWall(Vec2.Create(-40,0), Vec2.Create(10,80)));
    walls.Add(PongWall(Vec2.Create(40,0), Vec2.Create(10,80)));
    walls.Add(PongWall(Vec2.Create(0,-40), Vec2.Create(80,10)));
    walls.Add(PongWall(Vec2.Create(0,40), Vec2.Create(80,10)));
}

fun CreateBall(at, vel)
{
    ball = PongBall(at, vel);
}

fun CreatePaddles()
{
    leftPaddle = PongPaddle(
        Vec2.Create(-25,0),
        paddleMoveSpeed,
        ""w"",
        ""s""
    );
    rightPaddle = PongPaddle(
        Vec2.Create(25,0),
        paddleMoveSpeed,
        ""up"",
        ""down""
    );
}

fun Update()
{
    UpdateGame(dt);
}

fun UpdateGame(dt)
{
	if(GetKey(""escape"")){ReloadScene();}
    leftPaddle.Update();
    rightPaddle.Update();
}
";
            testEngine.Run(Vec2_ulox);
            testEngine.Run(Pong_mixins_ulox);
            testEngine.Run(Pong_ulox);

            Assert.AreEqual("", testEngine.InterpreterResult);
            var byteCounts = testEngine.MyEngine.Context.Program.CompiledScripts.SelectMany(x => x.AllChunks.Select(y => (y.Name, y.Instructions.Count))).ToArray();
            for (int i = 0; i < byteCounts.Length; i++)
            {
                Assert.AreEqual(unoptCounts[i].name, byteCounts[i].Name);
                Assert.GreaterOrEqual(unoptCounts[i].count, byteCounts[i].Count);
            }
        }
    }
}
