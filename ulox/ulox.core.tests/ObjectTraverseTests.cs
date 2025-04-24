using NUnit.Framework;

namespace ULox.Core.Tests
{
    public class ObjectTraverseTests : EngineTestBase
    {
        [Test]
        public void Update_WhenClassIsEmptyAndSameType_ShouldNotThrow()
        {
            testEngine.Run(@"
class Foo {}

var foo = Foo();
var foo2 = Foo();

foo = foo update foo2;
"
            );

            Assert.AreEqual("", testEngine.InterpreterResult);
        }
        
        [Test]
        public void Update_WhenClassIsEmptyAndNotSameType_ShouldNotThrow()
        {
            testEngine.Run(@"
class Foo {}

var foo = Foo();
var foo2 = {=};

foo = foo update foo2;
"
            );

            Assert.AreEqual("", testEngine.InterpreterResult);
        }
        
        [Test]
        public void Update_WhenClassIsNotEmptyAndDifferentAndSameType_ShouldUpdateValue()
        {
            testEngine.Run(@"
class Foo { var a; }

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
        public void Update_WhenNumber_ShouldUpdateValue()
        {
            testEngine.Run(@"
var foo = 1;
var foo2 = 2;

foo = foo update foo2;
print(foo);
");

            Assert.AreEqual("2", testEngine.InterpreterResult);
        }

        [Test]
        public void Update_WhenBool_ShouldUpdateValue()
        {
            testEngine.Run(@"
var foo = true;
var foo2 = false;

foo = foo update foo2;
print(foo);
");

            Assert.AreEqual("False", testEngine.InterpreterResult);
        }

        [Test]
        public void Update_WhenString_ShouldUpdateValue()
        {
            testEngine.Run(@"
var foo = ""Hello"";
var foo2 = ""World"";

foo = foo update foo2;
print(foo);
");

            Assert.AreEqual("World", testEngine.InterpreterResult);
        }

        [Test]
        public void Update_WhenNotSameValueType_ShouldNotUpdateValue()
        {
            testEngine.Run(@"
var foo = 1;
var foo2 = false;

foo = foo update foo2;
print(foo);
");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Update_WhenPartialMatch_ShouldUpdateValue()
        {
            testEngine.Run(@"
var foo = {a=1, b=2,};
var foo2 = {a=2, c=3,};

foo = foo update foo2;
print(foo.a);
print(foo.b);
print(foo meets foo2);
"
            );

            StringAssert.StartsWith("22False", testEngine.InterpreterResult);
        }

        [Test]
        public void Update_WhenMatchNameNull_ShouldUpdateValue()
        {
            testEngine.Run(@"
var foo = {a=1, b=2,};
var foo2 = {a=null, c=3,};

foo = foo update foo2;
print(foo.a);
print(foo.b);
print(foo meets foo2);
"
            );

            StringAssert.StartsWith("null2False", testEngine.InterpreterResult);
        }

        [Test]
        public void TraveseUpdate_WhenMatchNameNull_ShouldUpdateValue()
        {
            testEngine.Run(@"
var foo = {a=1, b=2,};
var foo2 = {a=null, c=3,};

fun Update(lhs, rhs)
{
    retval = rhs;
}

Object.TraverseUpdate(foo, foo2, Update);

printh(foo);
"
            );

            StringAssert.Contains("a:null", testEngine.InterpreterResult);
            StringAssert.Contains("b:2", testEngine.InterpreterResult);
        }

        [Test]
        public void Accum_WhenMatchNameNull_ShouldIncreaseValue()
        {
            testEngine.Run(@"
var foo = {a=1, b=2,};
var foo2 = {a=null, b=3,};

fun Accum(lhs, rhs)
{
    if (!lhs)
        lhs = 0;
    if (!rhs)
        rhs = 0;
    retval = lhs + rhs;
}

Object.TraverseUpdate(foo, foo2, Accum);

printh(foo);
"
            );

            StringAssert.Contains("a:1", testEngine.InterpreterResult);
            StringAssert.Contains("b:5", testEngine.InterpreterResult);
        }

        [Test]
        public void Scale_WhenMatchNameNull_ShouldIncreaseValue()
        {
            testEngine.Run(@"
var foo = {a=2, b=3,};
var foo2 = {a=0.5, b=2,};

fun Scale(lhs, rhs)
{
    if (!lhs)
        lhs = 1;
    if (!rhs)
        rhs = 1;
    retval = lhs * rhs;
}

Object.TraverseUpdate(foo, foo2, Scale);

printh(foo);
"
            );

            StringAssert.Contains("a:1", testEngine.InterpreterResult);
            StringAssert.Contains("b:6", testEngine.InterpreterResult);
        }

        [Test]
        public void Update_WhenPartialHierarchyMatch_ShouldUpdateValue()
        {
            testEngine.Run(@"
var foo = 
{
    a=
    {
        val=1
    }, 
    b=2,
    d= 
    {
        a=1, 
        b=2,
    }
};

var foo2 = 
{
    a=
    {
        val=2,
        otherVal=2,
    }, 
    c=3,
    d= 
    {
        a=2, 
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
var foo = {a=[], b=2, d = Map().CreateOrUpdate(1,1).CreateOrUpdate(2,2)};
var foo2 = {a=[1,2], c=3, d = Map().CreateOrUpdate(""a"",1).CreateOrUpdate(""b"",2).CreateOrUpdate(""c"",3)};

foo = foo update foo2;
print(foo.a.Count());
print(foo.d[""c""]);
print(foo meets foo2);
"
            );

            StringAssert.StartsWith("23False", testEngine.InterpreterResult);
        }

        [Test]
        public void Update_WhenInInit_ShouldUpdateValue()
        {
            testEngine.Run(@"
class Foo 
{ 
    var a;

    init()
    {
        this update {a = 1,};
    }
}

var foo = Foo();
print(foo.a);
");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Update_WhenMixedInAndExtendedWithNulls_ShouldUpdateAll()
        {
            testEngine.Run(@"
class Bar { var a,b; }
class Foo {mixin Bar; var c;}

var bar = Bar();
bar.a = 1;
bar.b = [];
var foo = Foo() update bar;
print(foo.a);
print(foo.b);
print(foo.c);
");

            Assert.AreEqual("1<inst NativeList>null", testEngine.InterpreterResult);
        }
    }
}