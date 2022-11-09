﻿using NUnit.Framework;

namespace ulox.core.tests
{
    public class DataTypeTests : EngineTestBase
    {
        [Test]
        public void Delcared_WhenAccessed_ShouldHaveDataObject()
        {
            testEngine.Run(@"
data Foo {}
print (Foo);");

            Assert.AreEqual("<Data Foo>", testEngine.InterpreterResult);
        }

        [Test]
        public void DataInstance_WhenAccessed_ShouldHaveInstanceOfObject()
        {
            testEngine.Run(@"
data Foo {}
var b = Foo();
print (b);");

            Assert.AreEqual("<inst Foo>", testEngine.InterpreterResult);
        }

        [Test]
        public void DataInstanceField_WhenAccessed_ShouldHaveDefaultValue()
        {
            testEngine.Run(@"
data Foo {A}
var b = Foo();
print (b.A);");

            Assert.AreEqual("null", testEngine.InterpreterResult);
        }

        [Test]
        public void DataInstanceField_WhenAccessed_ShouldHaveInitialiserValue()
        {
            testEngine.Run(@"
data Foo {A = true}
var b = Foo();
print (b.A);");

            Assert.AreEqual("True", testEngine.InterpreterResult);
        }

        [Test]
        public void DataInstanceFields_WhenAccessed_ShouldHaveInitialValues()
        {
            testEngine.Run(@"
data Foo {A = true, review, taste = ""Full""}
var b = Foo();
print (b.A);
print (b.review);
print (b.taste);");

            Assert.AreEqual("TruenullFull", testEngine.InterpreterResult);
        }

        [Test]
        public void OutOfOrder_WhenVarThenSigns_ShouldFail()
        {
            testEngine.Run(@"
data A{a; signs B;}");

            StringAssert.StartsWith("Stage out of order. Type 'A' is at stage 'Var' has encountered a late 'Signs' stage element in chunk", testEngine.InterpreterResult);
        }
    }
}