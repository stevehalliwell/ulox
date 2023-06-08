using NUnit.Framework;

namespace ULox.Core.Tests
{
    [TestFixture]
    public class MathTests : EngineTestBase
    {
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
    }
}
