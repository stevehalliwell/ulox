using NUnit.Framework;

namespace ULox.Core.Tests
{
    [TestFixture]
    public class BuildQueueTests
    {
        [Test]
        public void ctor_WhenDefault_ShouldNotThrow()
        {
            void Act () { new BuildQueue(); }

            Assert.DoesNotThrow(Act);
        }
        
        [Test]
        public void Enqueue_WhenDefault_ShouldNotThrow()
        {
            var queue =  new BuildQueue();

            void Act() { queue.Enqueue(default); }

            Assert.DoesNotThrow(Act);
        }

        [Test]
        public void HasItems_WhenEnqueued_ShouldReturnTrue()
        {
            var queue = new BuildQueue();
            queue.Enqueue(default);

            var res = queue.HasItems;

            Assert.IsTrue(res);
        }

        [Test]
        public void HasItems_Whenctor_ShouldReturnFalse()
        {
            var queue = new BuildQueue();

            var res = queue.HasItems;

            Assert.IsFalse(res);
        }
    }
}