using NUnit.Framework;

namespace ULox.Core.Tests
{
    [TestFixture]
    public class FastStackTests
    {
        [Test]
        public void Count_WhenEmpty_ShouldHave0()
        {
            var fastStack = new FastStack<object>();

            Assert.AreEqual(0, fastStack.Count);
        }

        [Test]
        public void Push_WhenCalledOnEmpty_ShouldHave1Count()
        {
            var fastStack = new FastStack<object>();

            fastStack.Push(new object());

            Assert.AreEqual(1, fastStack.Count);
        }

        [Test]
        public void Pop_WhenCalledOnOneElement_ShouldHave0Count()
        {
            var fastStack = new FastStack<object>();
            fastStack.Push(new object());

            fastStack.Pop();

            Assert.AreEqual(0, fastStack.Count);
        }

        [Test]
        public void Reset_WhenCalledOnOneElement_ShouldHave0Count()
        {
            var fastStack = new FastStack<object>();
            fastStack.Push(new object());

            fastStack.Reset();

            Assert.AreEqual(0, fastStack.Count);
        }

        [Test]
        public void Peek_WhenCalledOnOneElement_ShouldReturnObjectAndHave1Count()
        {
            var fastStack = new FastStack<object>();
            var givenObject = new object();
            fastStack.Push(givenObject);

            var result = fastStack.Peek();

            Assert.AreEqual(1, fastStack.Count);
            Assert.AreSame(givenObject, result);
        }

        [Test]
        public void Peek_WhenCalledOnTwoElementWithDownOne_ShouldReturn0ObjectAndHave2Count()
        {
            var fastStack = new FastStack<object>();
            var givenObject = new object();
            fastStack.Push(givenObject);
            fastStack.Push(new object());

            var result = fastStack.Peek(1);

            Assert.AreEqual(2, fastStack.Count);
            Assert.AreSame(givenObject, result);
        }

        [Test]
        public void DiscardPop_WhenCalledOnTwoElementWithDownTwo_ShouldHave0Count()
        {
            var fastStack = new FastStack<object>();
            var givenObject = new object();
            fastStack.Push(givenObject);
            fastStack.Push(new object());

            fastStack.DiscardPop(2);

            Assert.AreEqual(0, fastStack.Count);
        }

        [Test]
        public void SetAt_WhenCalledOnTwoElementWith0_ShouldReplaceObjectAtIndex()
        {
            var fastStack = new FastStack<object>();
            var givenObject = new object();
            fastStack.Push(new object());
            fastStack.Push(new object());

            fastStack.SetAt(0, givenObject);

            Assert.AreEqual(2, fastStack.Count);
            Assert.AreSame(givenObject, fastStack.Peek(1));
        }

        [Test]
        public void ArrayIndex_WhenCalledOnTwoElementWith0_ShouldReplaceObjectAtIndex()
        {
            var fastStack = new FastStack<object>();
            var givenObject = new object();
            fastStack.Push(new object());
            fastStack.Push(new object());

            fastStack[0] = givenObject;

            Assert.AreEqual(2, fastStack.Count);
            Assert.AreSame(givenObject, fastStack[0]);
        }

        [Test]
        public void Grow_WhenPushTwiceStartingCapByGrowFactor_ShouldNotThrow()
        {
            var fastStack = new FastStack<object>();

            for (int i = 0; i < FastStack<object>.StartingSize * FastStack<object>.GrowFactor * 2; i++)
            {
                fastStack.Push(new object());
            }

            Assert.Less(FastStack<object>.StartingSize, fastStack.Count);
        }
    }
}