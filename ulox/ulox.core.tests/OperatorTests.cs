using NUnit.Framework;

namespace ULox.Core.Tests
{
    [TestFixture]
    public class OperatorTests : EngineTestBase
    {
        [Test]
        public void Add_When1and2_ShouldPrint3()
        {
            testEngine.Run(@"
print(1+2);");
            Assert.AreEqual("3", testEngine.InterpreterResult);
        }

        [Test]
        public void Add_When1and2and3_ShouldPrint6()
        {
            testEngine.Run(@"
print(1+2+3);");
            Assert.AreEqual("6", testEngine.InterpreterResult);
        }

        [Test]
        public void Sub_When1and2_ShouldPrintNeg1()
        {
            testEngine.Run(@"
print(1-2);");
            Assert.AreEqual("-1", testEngine.InterpreterResult);
        }

        [Test]
        public void Sub_When1and2and3_ShouldPrintNeg4()
        {
            testEngine.Run(@"
print(1-2-3);");
            Assert.AreEqual("-4", testEngine.InterpreterResult);
        }

        [Test]
        public void Mul_When1and2_ShouldPrint2()
        {
            testEngine.Run(@"
print(1*2);");
            Assert.AreEqual("2", testEngine.InterpreterResult);
        }

        [Test]
        public void Mul_When1and2and3_ShouldPrint6()
        {
            testEngine.Run(@"
print(1*2*3);");
            Assert.AreEqual("6", testEngine.InterpreterResult);
        }

        [Test]
        public void Mul_When1and2and3and4_ShouldPrint24()
        {
            testEngine.Run(@"
print(1*2*3*4);");
            Assert.AreEqual("24", testEngine.InterpreterResult);
        }

        [Test]
        public void Div_When1and2_ShouldPrint05()
        {
            testEngine.Run(@"
print(1/2);");
            Assert.AreEqual("0.5", testEngine.InterpreterResult);
        }

        [Test]
        public void Mod_When1and2_ShouldPrint1()
        {
            testEngine.Run(@"
print(1%2);");
            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Mod_When2and3_ShouldPrint2()
        {
            testEngine.Run(@"
print(2%3);");
            Assert.AreEqual("2", testEngine.InterpreterResult);
        }

        [Test]
        public void Mod_When3and2_ShouldPrint1()
        {
            testEngine.Run(@"
print(3%2);");
            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Mod_WhenNeg2and3_ShouldPrint1()
        {
            testEngine.Run(@"
print(-2%3);");
            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [TestCaseSource(nameof(OperatorCases))]
        public void Op_WhenIncompatTypes_ShouldError(string op)
        {
            testEngine.Run($"var res = 1 {op} [];");
            StringAssert.StartsWith("Cannot perform op", testEngine.InterpreterResult);
        }

        [TestCaseSource(nameof(OperatorCases))]
        public void Op_WhenOrderInvertedIncompatTypes_ShouldError(string op)
        {
            testEngine.Run($"var res = [] {op} 1;");
            StringAssert.StartsWith("Cannot perform op", testEngine.InterpreterResult);
        }

        public static object[] OperatorCases()
        {
            return new object[]
            {
                new object[] { "+" },
                new object[] { "-" },
                new object[] { "*" },
                new object[] { "/" },
                new object[] { "%" },
                new object[] { ">" },
                new object[] { "<" },
            };
        }
    }
}
