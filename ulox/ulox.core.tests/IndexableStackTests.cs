using NUnit.Framework;
using System.Linq;

namespace ULox.Core.Tests
{
    [TestFixture]
    public class IndexableStackTests
    {
        [Test]
        public void Push_WhenCalled_ShouldIncreaseCount()
        {
            var ind = new IndexableStack<object>();
            var initialCount = ind.Count;
            ind.Push(new object());

            var newCount = ind.Count;
            var result = newCount - initialCount;

            Assert.AreEqual(1, result);
        }

        [Test]
        public void Peek_WhenTwoPushedCalled_ShouldReturnBackAndCountShouldBeTwo()
        {
            var ind = new IndexableStack<object>();
            var first = new object();
            var second = new object();
            ind.Push(first);
            ind.Push(second);

            var result = ind.Peek();

            Assert.AreEqual(second, result);
            Assert.AreEqual(second, ind.Last());
            Assert.AreEqual(2, ind.Count);
        }

        [Test]
        public void PeekDown_WhenTwoPushedCalledWith1_ShouldReturnFirst()
        {
            var ind = new IndexableStack<object>();
            var first = new object();
            var second = new object();
            ind.Push(first);
            ind.Push(second);

            var result = ind.Peek(1);

            Assert.AreEqual(first, result);
        }

        [Test]
        public void Pop_WhenTwoPushedCalled_ShouldReturnBackAndCountShouldBeOne()
        {
            var ind = new IndexableStack<object>();
            var first = new object();
            var second = new object();
            ind.Push(first);
            ind.Push(second);

            var result = ind.Pop();

            Assert.AreEqual(second, result);
            Assert.AreEqual(first, ind.Last());
            Assert.AreEqual(1, ind.Count);
        }
    }
}