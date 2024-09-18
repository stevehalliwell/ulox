using NUnit.Framework;

namespace ULox.Core.Tests
{
    public class DynamicTests : EngineTestBase
    {
        [Test]
        public void Fields_WhenAddedToDynamic_ShouldSucceed()
        {
            testEngine.Run(@"
var obj = {=};

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
var obj = {a=1,};
print(obj.a);
");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Fields_WhenAddedToDynamicInline_ShouldSucceed()
        {
            testEngine.Run(@"
var obj = {a=1, b=2, c=3, d=-1,};

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
var obj = {=};

print(obj);
");

            Assert.AreEqual("<inst Dynamic>", testEngine.InterpreterResult);
        }

        [Test]
        public void Dynamic_WhenInlineNested_ShouldPrint()
        {
            testEngine.Run(@"
var obj = {a=1, b={innerA=2,}, c=3,};

print(obj.a);
print(obj.b);
print(obj.b.innerA);
print(obj.c);
");

            Assert.AreEqual("1<inst Dynamic>23", testEngine.InterpreterResult);
        }

        [Test]
        public void Dynamic_WhenInvalid_ShouldFail()
        {
            testEngine.Run(@"
var obj = {7};
");

            StringAssert.StartsWith("Expect identifier or '=' after '{'", testEngine.InterpreterResult);
        }

        [Test]
        public void Dyanic_RemoveFieldWhenReadOnly_ShouldError()
        {
            testEngine.Run(@"
var expected = false;
var result = 0;
var obj = {=};
obj.a = 7;
readonly obj;

obj.RemoveField(obj, ""a"");");

            StringAssert.StartsWith("Cannot remove field from read only", testEngine.InterpreterResult);
        }

        [Test]
        public void DyanicProperty_GetExists_ShouldMatch()
        {
            testEngine.Run(@"
var foo = { a = 1 };
var res = foo[""a""];
print(res);");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void DyanicProperty_GetDoesNotExists_ShouldError()
        {
            testEngine.Run(@"
var foo = { a = 1 };
var res = foo[""b""];
print(res);");

            StringAssert.StartsWith("No field of name 'b' could be found on instance", testEngine.InterpreterResult);
        }

        [Test]
        public void DyanicProperty_GetInvalidType_ShouldError()
        {
            testEngine.Run(@"
var foo = 7;
var res = foo[""b""];
print(res);");

            StringAssert.StartsWith("Cannot perform get index on type 'Double'", testEngine.InterpreterResult);
        }

        [Test]
        public void DyanicProperty_SetExists_ShouldMatch()
        {
            testEngine.Run(@"
var foo = { a = 1 };
foo[""a""] = 2;
print(foo.a);");

            Assert.AreEqual("2", testEngine.InterpreterResult);
        }

        [Test]
        public void DyanicProperty_SetDoesNotExists_ShouldError()
        {
            testEngine.Run(@"
var foo = { a = 1 };
foo[""b""] = 2;
print(foo.a);");

            StringAssert.StartsWith("Attempted to create a new entry 'b' via Set.", testEngine.InterpreterResult);
        }

        [Test]
        public void DyanicProperty_SetInvalidType_ShouldError()
        {
            testEngine.Run(@"
var foo = 7;
foo[""b""] = 2;
print(foo.a);");

            StringAssert.StartsWith("Cannot perform set index on type 'Double'", testEngine.InterpreterResult);
        }

        [Test]
        public void DyanicProperty_MultipleGets_ShouldMatch()
        {
            testEngine.Run(@"
class ShipPartAdjust
{
var
    name = ""standard"",
}

class ShipBodyPart
{
mixin
    ShipPartAdjust;
    
var
    dragSharpness = 0.2,
}

class ShipWingPart
{
mixin
    ShipPartAdjust;

var
    gravity = 5,
}

class ShipEnginePart
{
    mixin
        ShipPartAdjust;
var
    accel = 20,
}

class ShipTailPart
{
mixin
    ShipPartAdjust;

var
    turnPerSecondThrust = 190,
}

class ShipWeaponPart
{
mixin
    ShipPartAdjust;

var
    fireRate = 0.05,
}

var shipData = 
{
    bodies = 
    {
        standard = ShipBodyPart(),
        slim = ShipBodyPart() update {name = ""slim""},
    },
    wings = 
    {
        standard = ShipWingPart(),
        slim = ShipWingPart() update {name = ""slim""},
    },
    engines = 
    {
        standard = ShipEnginePart(),
        slim = ShipEnginePart() update {name = ""slim""},
    },
    tails = 
    {
        standard = ShipTailPart(),
        slim = ShipTailPart() update {name = ""slim""},
    },
    weapons = 
    {
        standard = ShipWeaponPart(),
        slim = ShipWeaponPart() update {name = ""slim""},
    },
};

fun Lookup(bodyName, wingName, engineName, tailName, weaponName)
{
    var body = shipData.bodies[bodyName];
    var wing = shipData.wings[wingName];
    var engine = shipData.engines[engineName];
    var tail = shipData.tails[tailName];
    var weapon = shipData.weapons[weaponName];
    print(body);
    print(wing);
    print(engine);
    print(tail);
    print(weapon);
}

Lookup(""standard"", ""standard"", ""standard"", ""standard"", ""standard"");
");

            Assert.AreEqual("<inst ShipBodyPart><inst ShipWingPart><inst ShipEnginePart><inst ShipTailPart><inst ShipWeaponPart>", testEngine.InterpreterResult);
        }
    }
}