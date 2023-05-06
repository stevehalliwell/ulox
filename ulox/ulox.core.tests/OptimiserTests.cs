using NUnit.Framework;

namespace ULox.Core.Tests
{
    [TestFixture]
    public class OptimiserTests : EngineTestBase
    {
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            var opt = testEngine.MyEngine.Context.Program.Optimiser;
            opt.Enabled = true;
            opt.EnableRemoveUnreachableLabels = true;
            opt.EnableRegisterisation = true;
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
        public void Optimiser_RegisterableMath_CollapsesToRegisterBasedOp()
        {
            testEngine.Run(@"
{
var a = 1;
var b = 2;
var c = 0;
c = a + b;
print (c);
}");

            Assert.AreEqual("3", testEngine.InterpreterResult);
            Assert.AreEqual(12, testEngine.MyEngine.Context.Program.CompiledScripts[0].TopLevelChunk.Instructions.Count);
        }

        [Test]
        public void Optimiser_RegisterableCompare_CollapsesToRegisterBasedOp()
        {
            testEngine.Run(@"
{
var a = 1;
var b = 2;
var c = true;
c = a == b;
print (c);
}");

            Assert.AreEqual("False", testEngine.InterpreterResult);
            Assert.AreEqual(12, testEngine.MyEngine.Context.Program.CompiledScripts[0].TopLevelChunk.Instructions.Count);
        }

        [Test]
        public void Optimiser_ChainCalls_CollapsesToRegisterBasedOp()
        {
            testEngine.Run(@"
{
var a = 1;
var b = 2;
var c = 3;
var d = 0;
d = a+b-c;
print (d);
}");

            Assert.AreEqual("0", testEngine.InterpreterResult);
            Assert.Greater(17, testEngine.MyEngine.Context.Program.CompiledScripts[0].TopLevelChunk.Instructions.Count);
        }

        [Test]
        public void Optimiser_WaterLineSimplified_CollapsesToRegisterBasedOp()
        {
            testEngine.Run(@"
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

	loop(points)
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
}");
            
            Assert.Less(17, testEngine.MyEngine.Context.Program.CompiledScripts[0].TopLevelChunk.Instructions.Count);
        }
    }
}
