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
    pi = 1.618;
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

// multi var assignment wraps the declaring variables in '()'
var (x,y) = VecAdd(1,2,3,4);

// vars are dynamically typed, if no value is provided they will be 'null'
var somethingThatWillBeNull;

//strings are via ""
var someString = "hi";

//functions are also values
var someFunc = Foo;
// and invoked via ()
someFunc();

//strings can be combined with '+'
var someCombinedString = someString + " " + "there";

// native types for list, map, and object types
var aList = []; //an empty list
aList.Add(7);   //methods to add remove and the like.

var anotherList = [1,2,3,4,]; // inline declare the value of a list

var aMap = [:]; //an empty map
aMap.Create(1,"hi");    //CRUD methods on map 

var anotherMap = [  // inline map declare
    name:"ulox",
    type:"dynamic",
    ];

var dynObject = {=};    //an empty object
dynObject.a = 7;    // these objects are not 'frozen' so fields can be added and adjusted 

var anotherDyncObject = //an inline dynamic object 
{
    a = Foo,
    b = 7,
    c = "hi",
};

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

//ulox has match syntax for chaining single if == style logic, having no match will result in a runtime error
match a
{
    1: print(2);    //equiv to if (a == 1) {print(2);}
    0+2: print(1);  //equiv to if (a == (0+2)) 
    3:
    {
        //match can also have blocks if more than 1 statement is required
        var d = Foo();
        d += 7;
        print(d);
    }
}

//looping constructs
//while continues the block (or single statement) until the condition is false
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

//loop, without any following is an infinite loop, equiv to while(true)
loop
{
    break;
}

//loop with following info is for iterating over collections
var myArr = [1,2,3,4,5,];
loop (myArr)    // auto defines, item, i, count. In that order, you can provide custom names if desired or if nested
{
    print("Val '" + item + "' @ '" + i "' of " + count);
}

//user created data types are class, data, and system
//classes are the most flexible, data and system are more narrowly focused versions for 
class MyFoo
{
    // one more vars, must be declared first, any assigned values are calculated before init or returning the instance to the user
    var a,b = 3,c;

    //init is a special method called automatically when you create an instance, arity must match.
    // you do not have to define an init for your class, not doing so is equiv to init(){}
    // Sugar. Any arg matching a field name is automatically assigned to the field, so here the equiv of this.a = a; occurs automatically
    init(a)
    {
        // the instance being made is not frozen yet, you could add more fields to the type here, not recommended though.
    }

    //methods are the same as functions but they have access to the 'this', the instance the method is being invoked upon
    Bar(arg)
    {
        this.a = arg;
    }
}

//to get an instance of the class, we invoke it's name and match it's init args
var myFoo = MyFoo(1);

```