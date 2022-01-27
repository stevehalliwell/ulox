using NUnit.Framework;

namespace ULox.Tests
{
    public class DynamicTests : EngineTestBase
    {
        [Test]
        public void Fields_WhenAddedToDynamic_ShouldSucceed()
        {
            testEngine.Run(@"
var obj = {:};

obj.a = 1;
obj.b = 2;
obj.c = 3;
obj.d = -1;

var d = obj.a + obj.b + obj.c;
obj.d = d;

print(obj.d);
");

            Assert.AreEqual("6", testEngine.InterpreterResult);
        }

        [Test]
        public void Dynamic_WhenCreated_ShouldPrintInstType()
        {
            testEngine.Run(@"
var obj = {:};

print(obj);
");

            Assert.AreEqual("<inst Dynamic>", testEngine.InterpreterResult);
        }

        //        [Test]
        //        public void DynamicAsClass_WhenSetupAndCalled_ShouldPrintExepctedResult()
        //        {
        //            testEngine.Run(@"
        //class CoffeeMaker {
        //    Set(_coffee) {
        //        this.coffee = _coffee;
        //        return this;
        //    }

        //    brew() {
        //        print (""Enjoy your cup of "" + this.coffee);

        //        // No reusing the grounds!
        //        this.coffee = null;
        //    }
        //}

        //var maker = CoffeeMaker();
        //maker.Set(""coffee and chicory"");
        //maker.brew();");

        //            Assert.AreEqual("Enjoy your cup of coffee and chicory", testEngine.InterpreterResult);
        //        }
    }
}
