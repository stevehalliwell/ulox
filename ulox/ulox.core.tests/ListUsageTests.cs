﻿using NUnit.Framework;

namespace ulox.core.tests
{
    public class ListUsageTests : EngineTestBase
    {
        [Test]
        public void Engine_List()
        {
            testEngine.Run(@"
var list = [];

for(var i = 0; i < 5; i += 1)
    list.Add(i+1);

var c = list.Count();
print(c);

for(var i = 0; i < c; i += 1)
    print(list[i]);

for(var i = 0; i < c; i +=1)
    list[i] = -i-1;

for(var i = 0; i < c; i += 1)
    print(list[i]);
");

            Assert.AreEqual("512345-1-2-3-4-5", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_NativeList_Create()
        {
            testEngine.Run(@"
var arr = [];
print(arr);
");

            Assert.AreEqual("<inst NativeList>", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_NativeList_Count()
        {
            testEngine.Run(@"
var arr = [];
print(arr.Count());
");

            Assert.AreEqual("0", testEngine.InterpreterResult);
        }

        [Test]
        public void Count_WhenEmpty_ShouldBe0()
        {
            testEngine.Run(@"
var arr = [];
print(countof arr);
");

            Assert.AreEqual("0", testEngine.InterpreterResult);
        }

        [Test]
        public void Count_WhenNull_ShouldFail()
        {
            testEngine.Run(@"
var arr = null;
print(countof arr);
");

            StringAssert.StartsWith("Cannot perform countof on 'null' at ip:'8' in chunk:'unnamed_chunk(test:3)'.", testEngine.InterpreterResult);
        }

        [Test]
        public void Count_WhenNotEmpty_SohuldBe3()
        {
            testEngine.Run(@"
var arr = [1,2,3];
print(countof arr);
");

            Assert.AreEqual("3", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_NativeList_Add_CountInc()
        {
            testEngine.Run(@"
var arr = [];
arr.Add(1);
print(arr.Count());
");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_NativeList_Add_ValueMatches()
        {
            testEngine.Run(@"
var arr = [];
arr.Add(1);
print(arr[0]);
");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_NativeList_AddThenSet_NewValueMatches()
        {
            testEngine.Run(@"
var arr = [];
arr.Add(1);
arr[0] = 2;
print(arr[0]);
");

            Assert.AreEqual("2", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_NativeList_SameObjectAddThenRemove_CountIs0()
        {
            testEngine.Run(@"
var arr = [];
var obj = 1;
arr.Add(obj);
arr.Remove(obj);
print(arr.Count());
");

            Assert.AreEqual("0", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_NativeList_SameValueAddThenRemove_CountIs0()
        {
            testEngine.Run(@"
var arr = [];
arr.Add(2);
arr.Remove(2);
print(arr.Count());
");

            Assert.AreEqual("0", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_NativeList_Resize10_CountIs0()
        {
            testEngine.Run(@"
var arr = [];
arr.Resize(10, null);
print(arr.Count());
");

            Assert.AreEqual("10", testEngine.InterpreterResult);
        }

        [Test]
        public void Engine_NativeList_SetNonTrivial_ValueMatches()
        {
            testEngine.Run(@"
var arr = [];
arr.Resize(10, null);
arr[5] = 2*4;
print(arr[5]);
");

            Assert.AreEqual("8", testEngine.InterpreterResult);
        }

        [Test]
        public void ListLiteral_WhenSingleItemAndAssigned_ShouldMatchValues()
        {
            testEngine.Run(@"
var arr = [1];
print(arr[0]);
");

            Assert.AreEqual("1", testEngine.InterpreterResult);
        }

        [Test]
        public void ListLiteral_WhenThreeItemsAndAssigned_ShouldMatchValues()
        {
            testEngine.Run(@"
var arr = [1,2,3];
print(arr[0]);
print(arr[1]);
print(arr[2]);
");

            Assert.AreEqual("123", testEngine.InterpreterResult);
        }

        [Test]
        public void ListLiteral_WhenThreeItemsAndAssignedAndTrialingComma_ShouldMatchValues()
        {
            testEngine.Run(@"
var arr = [1,2,3,];
print(arr[0]);
print(arr[1]);
print(arr[2]);
");

            Assert.AreEqual("123", testEngine.InterpreterResult);
        }

        [Test]
        public void ListLiteral_WhenThreeNonTrivialItemsAndAssigned_ShouldMatchValues()
        {
            testEngine.Run(@"

fun GetThree {return 3;}

var arr = [1,(1+1),GetThree()];
print(arr[0]);
print(arr[1]);
print(arr[2]);
");

            Assert.AreEqual("123", testEngine.InterpreterResult);
        }

        [Test]
        public void Grow_When0_ShouldGiveEmpty()
        {
            testEngine.Run(@"
var list = [].Grow(0, 0);

print(list.Count());
");

            Assert.AreEqual("0", testEngine.InterpreterResult);
        }

        [Test]
        public void Grow_WhenFiveOnes_ShouldGiveFiveElementsOfOne()
        {
            testEngine.Run(@"
var list = [].Grow(5, 1);

print(list.Count());


fun accum(cur, running)
{
    return running + cur;
}

print(list.Reduce(accum));
");

            Assert.AreEqual("55", testEngine.InterpreterResult);
        }

        [Test]
        public void Grow_WhenAlreadyLarger_ShouldDoNothing()
        {
            testEngine.Run(@"
var list = [1,2,3].Grow(2, null);

print(list.Count());


fun accum(cur, running)
{
    return running + cur;
}

print(list.Reduce(accum));
");

            Assert.AreEqual("36", testEngine.InterpreterResult);
        }

        [Test]
        public void Shrink_When0_ShouldGiveEmpty()
        {
            testEngine.Run(@"
var list = [].Shrink(0);

print(list.Count());
");

            Assert.AreEqual("0", testEngine.InterpreterResult);
        }

        [Test]
        public void Shrink_When5To2_ShouldBeTwoFront()
        {
            testEngine.Run(@"
var list = [1,2,3,4,5,].Shrink(2);

print(list.Count());


fun accum(cur, running)
{
    return running + cur;
}

print(list.Reduce(accum));
");

            Assert.AreEqual("23", testEngine.InterpreterResult);
        }
    }
}