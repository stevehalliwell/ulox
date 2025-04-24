# ULox

[![Coverage Status](https://coveralls.io/repos/github/stevehalliwell/ulox/badge.svg?branch=main)](https://coveralls.io/github/stevehalliwell/ulox?branch=main) [![Tests](https://github.com/stevehalliwell/ulox/actions/workflows/tests.yml/badge.svg)](https://github.com/stevehalliwell/ulox/actions/workflows/tests.yml)

A scripting language and language playground, in C#, in Unity.

Originally grew out of the [lox](https://github.com/munificent/craftinginterpreters) implementation, I was playing around with, [here](https://github.com/stevehalliwell/ulox-work). It was so engaging, I wanted to keep using, and growing, the language.

If you want to know more about the language itself, the tests are the best way right now. They're in the ulox.core.tests project in the ulox folder.

## What's it look like

```js
// single line comments

/* multi line comments
are as expected
*/

// the language is imperative, statements are executed as encountered at top level

// variables are declared with 'var'
// numbers are 64bit floats 
var pi = 22/7; //ish

//multi declare
var e = 2.71828,
    phi = 1.618;
// ulox is not whitespace sensitive so this could be all one line or different intends.

// functions are declared with 'fun'
fun Foo() 
{
}
//and invoked via ()
Foo();

// function args are named only
fun DoubleIt(a)
{
    // return values are named, retval is the default for single returns
    retval = a * 2;
}

// multiple return values are named in brackets after the arg list
fun VecAdd(x1,y1,x2,y2) (x,y)
{
    x = x1+x2;
    y = y1+y2;
}

fun VecAddIfNotZero(x1,y1,x2,y2) (x,y)
{
    if(x1 == 0)
    {
        x = x1;
        y = y1;
        //return statement exits immediately.
        //  Since return values are named, what ever value
        //  they hold at the return are the return values
        return;
    }
    x = x1+x2;
    y = y1+y2;
}

// multi var assignment wraps the declaring variables in '()'
var (x,y) = VecAdd(1,2,3,4);

// vars are dynamically typed, if unassigned they are 'null'
var somethingThatWillBeNull;

//strings are via ""
var someString = "hi";

//functions are also values
var someFunc = Foo;
// and invoked via ()
someFunc();

//strings can be combined with '+'
var someCombinedString = someString + " " + "there";

//all strings are interpolated
//  within a string code between {} must be an expression 
//  and is desugared to string fragments plus-ed with
//  the expression.
var someName = "Steve";
var interpedCombined = "Hi {someName}";

//if we want the {, we escape it with \
var stringWithBrak = "this has a \{ in it."

// native types for list, map, and object types
var aList = []; //an empty list
aList.Add(7);   //methods to add remove and the like.

var anotherList = [1,2,3,4,]; // inline declare the value of a list

var aMap = [:]; //an empty map
aMap.Create(1,"hi");    //CRUD methods on map 

var dynObject = {=};    //an empty object
// these objects are not 'frozen' so fields can be added
dynObject.a = 7;

var anotherDynObject = //an inline dynamic object 
{
    a = Foo,
    b = 7,
    c = "hi",
};

//object type variables are referenced
var referenceToAnother = anotherDynObject;
//duplicate creates a deep copy
var dupOfOther = Duplicate(anotherDynObject);
//Object.TraverseUpdate will call for all matching elements 
//  in left hand side with the matching values from right side
//  here it changes the dupOfOther.a to the 7 in dynObject
dupOfOther = Object.TraverseUpdate(dupOfOther, dynObject, fun (lhs, rhs) {retval = rhs;});

//comparisons classics
3 == 2; // false
3 != 2; // true
3 > 2;  // true
3 < 2;  // false
2 <= 2; // true
2 >= 2; // true
true and true; // true
true and false; // false
true or false; // true

//if classics 
if(1 < a and b != null)
{
} 
else if (c != null or IsTuesday())
{
}
else
{
}

//ulox has match syntax for chaining single if == style logic, 
//  having no match will result in a runtime error
match a
{
    1: print(2);    //equiv to if (a == 1) {print(2);}
    0+2: print(1);  //equiv to if (a == (0+2)) 
    3:
    {
        //match can also have blocks 
        //   when more than 1 statement is required
        var d = Foo();
        d += 7;
        print(d);
    }
}

//looping constructs
//while continues the block (or single statement)
//  until the condition is false
while(true)
{
    break;  //go to end of containing loop
}

//for is declare ; condition; post step, each is optional
for(var i = 0; i < 10 ; i += 1)
{
    if(i % 3 == 0)
        continue;   //skip to next 
}

//loop, without anything following is equiv to while(true)
loop
{
    break;
}

//loop with following info is for iterating over collections
var myArr = [1,2,3,4,5,];
loop myArr    
// the above auto defines, i, item, icount. You can optionally
//  provide a name for i, e.g."j" gives, jtem, jcount.   
{
    print("Val " + item + " @ " + i " of " + count);
}

//user created data types are enum, class

//enums are named values
//  similar in intent and usage to c style languages
enum Foo
{
    //each enum is a key and value, 
    //  That value can be anything. 
    //  If no values are assigned they auto increment from 0
    Bar = 7,
    Baz = 8,
}

//a class with data only is the most straight forward, 
//  it is simply a named prototype for a collection of vars
class Addresss
{
var
//as with other vars they can be given a starting value
    number = 0, 
    street,
    state,
    postcode,
    country 
// class var syntax is very tolerant, 
//  you can leave dangling commas or end with a ;
//  This makes moving from code made around a dynamic to
//  a class very straightforward.
}

var anAddress = Address();
anAddress.number = 7;

//classes are the feature rich.
//  The order of elements declared within the class is enforced
class MyFoo
{
    // one more vars, must be declared first, 
    //  any assigned values are calculated before init 
    //  or returning the instance to the user
    var a,b = 3,c;

    //init is a special method called automatically 
    //  when you create an instance, arity must match.
    // you do not have to define an init for your class, 
    //  not doing so is equiv to init(){}
    // Sugar. Any arg matching a field name is automatically 
    //  assigned to the field, 
    //  so here the equiv of this.a = a; occurs automatically
    init(a)
    {
    }

    //methods are the same as functions 
    //  but they have access to the 'this', 
    //  the instance the method is being invoked upon
    Bar(arg)
    {
        this.a = arg;
    }

    //you can omit the this. for fields of the instance
    Baz(arg)
    {
        a = arg;
    }
}

//to get an instance of the class, 
//   we invoke it's name and match it's init args
var myFoo = MyFoo(1);

//Classes can be used without instances 
//  if they don't use this or if the method is marked static
class FooSystem
{
    Bar(someState, dt)
    {
        //does stuff with args passed to it, has no this
    }
}

FooSystem.Bar(a, 0.1);

//ulox does not have inheritance, it does have mixins. 
//  These combine the elements of the type into the containing one.

class ThenSome
{
    var d;

    MoreBar(arg)
    {
        //...
    }
}

class MyFooAndThenSome
{
    //this class will have all the vars (including default values),
    //  and methods of both named mixins.
    mixin 
        MyFoo,
        ThenSome;

    var alsoMyOwnList = [];

    // mixins are pretty smart, 
    //  this will auto assign to the a we got from MyFoo, 
    //  the d from ThenSome, and our declared alsoMyOwnList. 
    init(a, d, alsoMyOwnList){}

    //...
}

//Since we don't have inheritance, we have operators
//   to check that objects have partial matching shapes.
//  meets returns a bool checking lhs has all the parts of rhs
var matchingShape1 = anAddress meets Address(); //will be true
var matchingShape2 = anAddress meets myFoo(); //will be false

//ulox also supports the use of fields via subscript syntax.
//  This allows for code that does not know the identifier ahead
//  of time to be written.
var someFieldNameWeDidntKnow = "a";
myFoo[someFieldNameWeDidntKnow] = 7; 

//testing is built into the language, 
//  they are setup like test fixtures with test cases.
//  They are auto run by the vm, in an isolated inner vm. 
//testset can have name, here FooTests, but the name is optional
testset FooTests
{
    test ForceFail
    {
        //throw keyword can be used anywhere, 
        //  it will stop the vm, 
        //  it can have an arg or message
        //Tests are run in an isolated inner vm, 
        //  the vm running the tests continues, but knows 
        //  the test failed.
        throw;
    }

    test AreEqual
    {
        //there are many Assert methods, 
        //  this means you don't need to if something throw
        Assert.AreEqual(1,1);
    }

    test ExpectEqual
    {
        //expect allows for chaining multiple asserts
        //  It will desugar to a throw with a message
        //  You can specify the message as a string
        //  after the expression with : "literal did not match"
        expect 
            1 == 1,
            2 == 2, //trailing commas are allowed
            ;
    }
    
    // test cases can also have data provided, 
    //  here we have 2 sets of data, this
    //the test will run twice once for each 
    //  set of values 
    //The values are expanded out to the args of the test method
    test ([ [1,2,3], [1,1,2] ]) Addition(a,b, expected)
    {
        var result = a+b;

        expect result == expected : "This custom message shown on failure, is optional";
    }
}
```
