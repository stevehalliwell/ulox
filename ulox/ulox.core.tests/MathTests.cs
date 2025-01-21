using NUnit.Framework;

namespace ULox.Core.Tests
{
    [TestFixture]
    public class MathTests : EngineTestBase
    {
        [Test]
        public void Literal_WhenDouble_ShouldNotLosePrecision()
        {
            testEngine.Run(@"
var a = 10000000.01;
var b = 10000000.02;
var c = a+b;
print(c);
");

            Assert.AreEqual("20000000.03", testEngine.InterpreterResult);
        }

        [Test]
        public void Rand_WhenNext_ShouldReturn01()
        {
            testEngine.Run(@"
var r = Math.Rand();
expect r < 1,
    r >= 0;");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void RandRange_When12_ShouldReturn12()
        {
            testEngine.Run(@"
var r = Math.RandRange(1,2);
expect r < 2,
    r >= 1;");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Sin_When25_ShouldReturn()
        {
            testEngine.Run(@"
var rads = Math.Deg2Rad(25);
print(Math.Sin(rads));");

            Assert.AreEqual("0.42261826174069944", testEngine.InterpreterResult);
        }

        [Test]
        public void Cos_When25_ShouldReturn()
        {
            testEngine.Run(@"
var rads = Math.Deg2Rad(25);
print(Math.Cos(rads));");

            Assert.AreEqual("0.9063077870366499", testEngine.InterpreterResult);
        }

        [Test]
        public void SinCos_When25_ShouldReturn()
        {
            testEngine.Run(@"
var rads = Math.Deg2Rad(25);
var (s,c) = Math.SinCos(rads);
print(s);
print(c);
");

            Assert.AreEqual("0.422618261740699440.9063077870366499", testEngine.InterpreterResult);
        }

        [Test]
        public void Tan_When25_ShouldReturn()
        {
            testEngine.Run(@"
var rads = Math.Deg2Rad(25);
print(Math.Tan(rads));");

            StringAssert.StartsWith("0.466307658", testEngine.InterpreterResult);
        }

        [Test]
        public void Pi_WhenUsed_ShouldMatchExpected()
        {
            testEngine.Run(@"
var s = Math.Sin(Math.Pi()/4);
var c = Math.Cos(Math.Pi()/4);
print(s-c);
");

            Assert.AreEqual(0, double.Parse(testEngine.InterpreterResult), 0.00000001);
        }

        [Test]
        public void Rad2Deg_When2Pi_ShouldBe360()
        {
            testEngine.Run(@"
var rads = Math.Pi()*2;
print(Math.Rad2Deg(rads));
");

            Assert.AreEqual("360", testEngine.InterpreterResult);
        }

        [Test]
        public void Sqrt_When4_ShouldBe2()
        {
            testEngine.Run(@"
print(Math.Sqrt(4));
");

            Assert.AreEqual("2", testEngine.InterpreterResult);
        }

        [Test]
        public void Pow_When33_ShouldBe27()
        {
            testEngine.Run(@"
print(Math.Pow(3,3));
");

            Assert.AreEqual("27", testEngine.InterpreterResult);
        }

        [Test]
        public void Exp_When3_ShouldBe20something()
        {
            testEngine.Run(@"
print(Math.Exp(3));
");

            Assert.AreEqual("20.085536923187668", testEngine.InterpreterResult);
        }

        [Test]
        public void Ln_WhenE_ShouldBe1()
        {
            testEngine.Run(@"
print(Math.Ln(Math.E()));
");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Log_When100and10_ShouldBe10()
        {
            testEngine.Run(@"
print(Math.Log(100,10));
");

            Assert.AreEqual("2", testEngine.InterpreterResult);
        }

        [Test]
        public void Atan2_When5andNeg5_ShouldBe()
        {
            testEngine.Run(@"
print(Math.Rad2Deg(Math.Atan2(5,-5)));
");

            Assert.AreEqual("135", testEngine.InterpreterResult);
        }

        [Test]
        public void Acos_WhenSin10_ShouldBe80()
        {
            testEngine.Run(@"
print(Math.Rad2Deg(Math.Acos(Math.Sin(Math.Deg2Rad(10)))));
");

            Assert.AreEqual("80", testEngine.InterpreterResult);
        }

        [Test]
        public void Atan_WhenTan10_ShouldBe10()
        {
            testEngine.Run(@"
print(Math.Rad2Deg(Math.Atan(Math.Tan(Math.Deg2Rad(10)))));
");

            Assert.AreEqual("10", testEngine.InterpreterResult);
        }

        [Test]
        public void Asin_WhenCos10_ShouldBe80()
        {
            testEngine.Run(@"
var asinRes = Math.Rad2Deg(Math.Asin(Math.Cos(Math.Deg2Rad(10))));
var target = 80;
var dif = asinRes - 80;
expect dif < 0.0001,
    dif > -0.0001;
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Floor_When5point5_ShouldBe5()
        {
            testEngine.Run(@"
var res = Math.Floor(5.5);
expect res == 5;
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Ceil_When5point5_ShouldBe6()
        {
            testEngine.Run(@"
var res = Math.Ceil(5.5);
expect res == 6;
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Round_WhenLower_ShouldBeFloor()
        {
            testEngine.Run(@"
var res = Math.Round(5.4);
expect res == 5;
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Round_WhenUpper_ShouldBeCeil()
        {
            testEngine.Run(@"
var res = Math.Round(5.6);
expect res == 6;
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void AbsSign_WhenPositive_Should1()
        {
            testEngine.Run(@"
var (v,s) = Math.AbsSign(-2);
expect 
    s == -1,
    v == 2;
;
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Sign_WhenPositive_Should1()
        {
            testEngine.Run(@"
var res = Math.Sign(1);
expect res == 1;
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Sign_WhenNegative_ShouldMinus1()
        {
            testEngine.Run(@"
var res = Math.Sign(-1);
expect res == -1;
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Max_When1And2_ShouldBe2()
        {
            testEngine.Run(@"
var res = Math.Max(1,2);
expect res == 2;
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Min_When1And2_ShouldBe1()
        {
            testEngine.Run(@"
var res = Math.Min(1,2);
expect res == 1;
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Clamp_When0And1And2_ShouldBe1()
        {
            testEngine.Run(@"
var res = Math.Clamp(0,1,2);
expect res == 1;
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Remap_WhenExpected_ShouldBe2()
        {
            testEngine.Run(@"
var res = Math.Remap(0.5, 0, 1, 1, 3);
expect res == 2;
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Remap_When0And1And2_ShouldBe1()
        {
            testEngine.Run(@"
var res = Math.Remap(0.5, 0, 1, 1, 3);
expect res == 2;
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void RandUnitCircle_WhenCalled_ShouldBeLessThan1Radius()
        {
            testEngine.Run(@"
var (x,y) = Math.RandUnitCircle();
var res = Math.Sqrt(x*x + y*y);
expect res < 2;
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void MoveTowards_When1to2and3_ShouldBe2()
        {
            testEngine.Run(@"
var res = Math.MoveTowards(1,2,3);
expect res == 2;
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void MoveTowards_When1to2and0dot5_ShouldBe1dot5()
        {
            testEngine.Run(@"
var res = Math.MoveTowards(1,2,0.5);
expect res == 1.5;
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Lerp_When1to2andQuarter_ShouldBe1andQuarter()
        {
            testEngine.Run(@"
var res = Math.Lerp(1,2,0.25);
expect res == 1.25;
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Dampen_When1and2_ShouldBeCloser()
        {
            testEngine.Run(@"
var a = 1;
var b = 2;
var decay = Math.CalcDampenHalflife(1,0.1);
var c = Math.Dampen(a,b,decay,0.1);
expect 
    c > a,
    c < b;
");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }
    }
}
