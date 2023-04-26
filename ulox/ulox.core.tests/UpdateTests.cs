using NUnit.Framework;

namespace ULox.Core.Tests
{
    public class UpdateTests : EngineTestBase
    {
        [Test]
        public void Update_WhenDataIsEmptyAndSameType_ShouldNotThrow()
        {
            testEngine.Run(@"
data Foo {}

var foo = Foo();
var foo2 = Foo();

foo = foo update foo2;
"
            );

            Assert.AreEqual("", testEngine.InterpreterResult);
        }
        
        [Test]
        public void Update_WhenDataIsEmptyAndNotSameType_ShouldNotThrow()
        {
            testEngine.Run(@"
data Foo {}

var foo = Foo();
var foo2 = {:};

foo = foo update foo2;
"
            );

            Assert.AreEqual("", testEngine.InterpreterResult);
        }
        
        [Test]
        public void Update_WhenDataIsNotEmptyAndDifferentAndSameType_ShouldUpdateValue()
        {
            testEngine.Run(@"
data Foo { a }

var foo = Foo();
foo.a = 1;
var foo2 = Foo();
foo2.a = 2;

foo = foo update foo2;
print(foo.a);
"
            );

            Assert.AreEqual("2", testEngine.InterpreterResult);
        }

        [Test]
        public void Update_WhenSameValueType_ShouldUpdateValue()
        {
            testEngine.Run(@"
var foo = 1;
var foo2 = 2;

foo = foo update foo2;
print(foo);
"
            );

            Assert.AreEqual("2", testEngine.InterpreterResult);
        }

        [Test]
        public void Update_WhenNotSameValueType_ShouldNotUpdateValue()
        {
            testEngine.Run(@"
var foo = 1;
var foo2 = false;

foo = foo update foo2;
print(foo);
"
            );

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Update_WhenPartialMatch_ShouldUpdateValue()
        {
            testEngine.Run(@"
var foo = {a:1, b:2,};
var foo2 = {a:2, c:3,};

foo = foo update foo2;
print(foo.a);
print(foo.b);
print(foo meets foo2);
"
            );

            StringAssert.StartsWith("22False", testEngine.InterpreterResult);
        }
        
        [Test]
        public void Update_WhenPartialHierarchyMatch_ShouldUpdateValue()
        {
            testEngine.Run(@"
var foo = 
{
    a:
    {
        val:1
    }, 
    b:2,
    d: 
    {
        a:1, 
        b:2,
    }
};

var foo2 = 
{
    a:
    {
        val:2,
        otherVal:2,
    }, 
    c:3,
    d: 
    {
        a:2, 
    }
};

foo = foo update foo2;
print(foo.a.val);
print(foo.d.a);
print(foo meets foo2);
"
            );

            StringAssert.StartsWith("22False", testEngine.InterpreterResult);
        }


        [Test]
        public void Update_WhenPartialWithNativeCollections_ShouldUpdateValue()
        {
            testEngine.Run(@"
var foo = {a:[], b:2, d:[1:1, 2:2,]};
var foo2 = {a:[1,2], c:3, d:[""a"":1, ""b"":2, ""c"":3,]};

foo = foo update foo2;
print(foo.a.Count());
print(foo.d[""c""]);
print(foo meets foo2);
"
            );

            StringAssert.StartsWith("23False", testEngine.InterpreterResult);
        }
    }
}
