using NUnit.Framework;

namespace ULox.Core.Tests
{
    public class ClassCTTITests : EngineTestBase
    {
        [Test]
        public void EmptyClass_WhenCorrect_ShouldHave1UserType()
        {
            testEngine.Run(@"
class Foo{};");

            Assert.AreEqual(1, testEngine.MyEngine.Context.Program.TypeInfo.UserTypeCount);
            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void EmptyClass_WhenExists_ShouldHaveMatchingName()
        {
            testEngine.Run(@"
class Foo{};");

            Assert.IsNotNull(testEngine.MyEngine.Context.Program.TypeInfo.GetUserType("Foo"));
            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void EmptyClass_WhenNonMatchingName_ShouldThrow()
        {
            testEngine.Run(@"
class Foo{};");

            Assert.Throws<UloxException>(() =>
                testEngine.MyEngine.Context.Program.TypeInfo.GetUserType("Bar"));
        }

        [Test]
        public void Method_WhenFetched_ShouldHaveMatchingMethodName()
        {
            testEngine.Run(@"
class Foo
{
    Bar{}
};");

            var ctti = testEngine.MyEngine.Context.Program.TypeInfo.GetUserType("Foo");
            Assert.AreEqual(1, ctti.Methods.Count);
            Assert.AreEqual("Bar", ctti.Methods[0].ChunkName);
            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void init_WhenFetched_ShouldHaveMatchingMethodName()
        {
            testEngine.Run(@"
class Foo
{
    init{}
};");

            var ctti = testEngine.MyEngine.Context.Program.TypeInfo.GetUserType("Foo");
            Assert.AreEqual(1, ctti.Methods.Count);
            Assert.AreEqual("init", ctti.Methods[0].ChunkName);
            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Field_WhenFetched_ShouldHaveMatchingFieldName()
        {
            testEngine.Run(@"
class Foo
{
    var bar;
};");

            var ctti = testEngine.MyEngine.Context.Program.TypeInfo.GetUserType("Foo");
            Assert.AreEqual(1, ctti.Fields.Count);
            Assert.AreEqual("bar", ctti.Fields[0]);
            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Mixin_WhenFetched_ShouldHaveMatchingMixinName()
        {
            testEngine.Run(@"
class Foo
{
};

class Bar
{
    mixin Foo;
};");

            var ctti = testEngine.MyEngine.Context.Program.TypeInfo.GetUserType("Bar");
            Assert.AreEqual(1, ctti.Mixins.Count);
            Assert.AreEqual("Foo", ctti.Mixins[0].Name);
            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Mixin_WhenField_ShouldHaveMatchingFieldFromFlavor()
        {
            testEngine.Run(@"
class Foo
{
    var a;
};

class Bar
{
    mixin Foo;
};");

            var ctti = testEngine.MyEngine.Context.Program.TypeInfo.GetUserType("Bar");
            Assert.AreEqual(1, ctti.Fields.Count);
            Assert.AreEqual("a", ctti.Fields[0]);
            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Mixin_WhenMethod_ShouldHaveMatchingMethodFromFlavor()
        {
            testEngine.Run(@"
class Foo
{
    Meth{}
};

class Bar
{
    mixin Foo;
};");

            var ctti = testEngine.MyEngine.Context.Program.TypeInfo.GetUserType("Bar");
            Assert.AreEqual(1, ctti.Methods.Count);
            Assert.AreEqual("Meth", ctti.Methods[0].ChunkName);
            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Signs_WhenMatchingField_ShouldHaveMatchingNameContract()
        {
            testEngine.Run(@"
class Foo
{
    var a;
};

class Bar
{
    signs Foo;
    var a;
};");

            Assert.AreEqual("", testEngine.InterpreterResult);
            var ctti = testEngine.MyEngine.Context.Program.TypeInfo.GetUserType("Bar");
            Assert.AreEqual(1, ctti.Contracts.Count);
            Assert.AreEqual("Foo", ctti.Contracts[0]);
        }

        [Test]
        public void Signs_WhenMatchingField_ShouldPass()
        {
            testEngine.Run(@"
class Foo
{
    var a;
};

class Bar
{
    signs Foo;
    var a;
};");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Signs_WhenMixedIn_ShouldPass()
        {
            testEngine.Run(@"
class Foo
{
    var a;
};

class Bar
{
    mixin Foo;
    signs Foo;
};");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Signs_WhenNoMatchingField_ShouldFailCompile()
        {
            testEngine.Run(@"
class Foo
{
    var a;
};

class Bar
{
    signs Foo;
};");

            StringAssert.StartsWith("Class 'Bar' does not meet contract 'Foo'. Type 'Bar' does not contain matching field 'a'", testEngine.InterpreterResult);
        }
    }
}