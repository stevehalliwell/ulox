using NUnit.Framework;

namespace ulox.core.tests
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
        public void Field_WhenSingleDynamicInline_ShouldSucceed()
        {
            testEngine.Run(@"
var obj = {a:1,};
print(obj.a);
");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Fields_WhenAddedToDynamicInline_ShouldSucceed()
        {
            testEngine.Run(@"
var obj = {a:1, b:2, c:3, d:-1,};

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

        [Test]
        public void Dynamic_WhenInlineNested_ShouldPrint()
        {
            testEngine.Run(@"
var obj = {a:1, b:{innerA:2,}, c:3,};

print(obj.a);
print(obj.b);
print(obj.b.innerA);
print(obj.c);
");

            Assert.AreEqual("1<inst Dynamic>23", testEngine.InterpreterResult);
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
