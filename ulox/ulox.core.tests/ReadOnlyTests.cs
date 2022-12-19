using NUnit.Framework;

namespace ulox.core.tests
{
    public class ReadOnlyTests : EngineTestBase
    {
        [Test]
        public void ReadOnly_StaticThenModify_Error()
        {
            testEngine.Run(@"
class Foo
{
    static var a = 1;
}

readonly Foo;
Foo.a = 2;
"
            );

            StringAssert.StartsWith("Attempted to Set field 'a', but instance is read only.", testEngine.InterpreterResult);
        }
        
        [Test]
        public void ReadOnly_InstThenModify_Error()
        {
            testEngine.Run(@"
class Foo
{
    var a = 1;
}

var f = Foo();
readonly f;
f.a = 2;
"
            );

            StringAssert.StartsWith("Attempted to Set field 'a', but instance is read only.", testEngine.InterpreterResult);
        }

        [Test]
        public void Dynamic_ReadOnlyHierarchyThenModifyInner_ShouldError()
        {
            testEngine.Run(@"
var obj = {a:1, b:{innerA:2,}, c:3,};

readonly obj;
obj.b.innerA = 4;
");

            StringAssert.StartsWith("Attempted to Set field 'innerA', but instance is read only.", testEngine.InterpreterResult);
        }

        [Test]
        public void ReadOnly_Local_ShouldError()
        {
            testEngine.Run(@"
var a = 1;
readonly a;
a = 2;
");

            StringAssert.StartsWith("Cannot perform readonly on '1'. Got unexpected type 'Double'", testEngine.InterpreterResult);
        }
    }
}
//multi expect