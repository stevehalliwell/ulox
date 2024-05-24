﻿using NUnit.Framework;

namespace ULox.Core.Tests
{
    public class SoaTests : EngineTestBase
    {
        [Test]
        public void Archetype_WhenManual()
        {
            testEngine.Run(@"
class Foo 
{
var 
    a = 1,
    b = 2
    ;
}

class FooSoa
{
var
    a = [],
    b = [],
    ;

    Add(fooItem)
    {
        a.Add(fooItem.a);
        b.Add(fooItem.b);
    }

    Count()
    {
        retval = a.Count();
    }

    RemoveAt(index)
    {
        a.RemoveAt(index);
        b.RemoveAt(index);
    }

    Clear()
    {
        a.Clear();
        b.Clear();
    }
}
");
            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Archetype_WhenAuto()
        {
            testEngine.Run(@"
class Foo 
{
var 
    a = 1,
    b = 2
    ;
}

soa FooSoa
{
    Foo,
}
");
            Assert.AreEqual("", testEngine.InterpreterResult);
        }

        [Test]
        public void Soa_WhenEmptyCount_0()
        {
            testEngine.Run(@"
class Foo 
{
var 
    a = 1,
    b = 2
    ;
}

soa FooSoa
{
    Foo,
}

var fooSoa = FooSoa();
var count = fooSoa.Count();
print(count);
");
            Assert.AreEqual("0", testEngine.InterpreterResult);
        }

        [Test]
        public void Soa_Add1_Count1()
        {
            testEngine.Run(@"
class Foo 
{
var 
    a = 1,
    b = 2
    ;
}

soa FooSoa
{
    Foo,
}

var fooSoa = FooSoa();
var foo = Foo();
fooSoa.Add(foo);
var count = fooSoa.Count();
print(count);
");
            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Soa_Add1ThenClear_Count0()
        {
            testEngine.Run(@"
class Foo 
{
var 
    a = 1,
    b = 2
    ;
}

soa FooSoa
{
    Foo,
}

var fooSoa = FooSoa();
var foo = Foo();
fooSoa.Add(foo);
fooSoa.Clear();
var count = fooSoa.Count();
print(count);
");
            Assert.AreEqual("0", testEngine.InterpreterResult);
        }

        [Test]
        public void Soa_Add1ThenRemoveAt0_Count0()
        {
            testEngine.Run(@"
class Foo 
{
var 
    a = 1,
    b = 2
    ;
}

soa FooSoa
{
    Foo,
}

var fooSoa = FooSoa();
var foo = Foo();
fooSoa.Add(foo);
fooSoa.RemoveAt(0);
var count = fooSoa.Count();
print(count);
");
            Assert.AreEqual("0", testEngine.InterpreterResult);
        }
    }
}
