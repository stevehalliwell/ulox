using NUnit.Framework;

namespace ULox.Core.Tests
{
    public class StringTests : EngineTestBase
    {
        [Test]
        public void str_WhenConcat_ShouldMatch()
        {
            testEngine.Run(@"
var a = 3;
var b = ""Foo"";
print(str(a)+str(b));");

            Assert.AreEqual("3Foo", testEngine.InterpreterResult);
        }

//        [Test]
//        public void str_WhenOverloaded_ShouldUseOverload()
//        {
//            testEngine.Run(@"
//class Foo
//{
//    var a=1, b=2;
    
//    _str(self) { retval = str(a) + "","" + str(b); }
//}

//var f = Foo();
//print(str(f));");

//            Assert.AreEqual("1,2", testEngine.InterpreterResult);
//        }

        [Test]
        public void Interpolate_WhenLitteral_ShouldPrint3()
        {
            testEngine.Run(@"
var s = ""Hi, {3}"";
print(s);");

            Assert.AreEqual("Hi, 3", testEngine.InterpreterResult);
        }

        [Test]
        public void Interpolate_WhenEscaped_ShouldPrintBrace()
        {
            testEngine.Run(@"
var s = ""Hi, \{3}"";
print(s);");

            Assert.AreEqual("Hi, {3}", testEngine.InterpreterResult);
        }

        [Test]
        public void Interpolate_WhenGlobal_ShouldPrint3()
        {
            testEngine.Run(@"
var n = 3;
var s = ""Hi, {n}"";
print(s);");

            Assert.AreEqual("Hi, 3", testEngine.InterpreterResult);
        }

        [Test]
        public void Interpolate_WhenFunc_ShouldPrint3()
        {
            testEngine.Run(@"
fun Foo() {retval = 3;}
var s = ""Hi, {Foo()}"";
print(s);");

            Assert.AreEqual("Hi, 3", testEngine.InterpreterResult);
        }

        [Test]
        public void Interpolate_WhenExpression_ShouldPrint3()
        {
            testEngine.Run(@"
var s = ""Hi, {1+2}"";
print(s);");

            Assert.AreEqual("Hi, 3", testEngine.InterpreterResult);
        }

        [Test]
        public void Interpolate_WhenMultipleInterpolatesAndExpression_ShouldPrint314()
        {
            testEngine.Run(@"
var s = ""Hi, {1+2}.{7+7}"";
print(s);");

            Assert.AreEqual("Hi, 3.14", testEngine.InterpreterResult);
        }

        [Test]
        public void Explode_WhenEmpty_ShouldReturnEmpty()
        {
            testEngine.Run(@"
var arr = String.Explode("""","""");
print(arr.Count());");

            Assert.AreEqual("0", testEngine.InterpreterResult);
        }

        [Test]
        public void Explode_WhenWords_ShouldReturnWords()
        {
            testEngine.Run(@"
var arr = String.Explode(""hello world"","" "");
print(arr.Count());
loop arr {print(item);}");

            Assert.AreEqual("2helloworld", testEngine.InterpreterResult);
        }
    }
}
