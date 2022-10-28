using NUnit.Framework;
using ULox;

namespace ulox.core.tests
{
    [TestFixture]
    public class HashStringTests
    {
        [Test]
        public void Hash_WhenSameUnderlyingString_ShouldMatch()
        {
            var targetString = "asdf";
            var hashed1 = new HashedString(targetString);
            var hashed2 = new HashedString(targetString);

            var result = hashed1.Hash == hashed2.Hash;

            Assert.IsTrue(result);
        }

        [Test]
        public void String_WhenSameUnderlyingString_ShouldMatch()
        {
            var targetString = "asdf";
            var hashed1 = new HashedString(targetString);
            var hashed2 = new HashedString(targetString);

            var result = hashed1.String == hashed2.String;

            Assert.IsTrue(result);
        }

        [Test]
        public void Equality_WhenSameObject_ShouldReturnTrue()
        {
            var targetString = "asdf";
            var hashed1 = new HashedString(targetString);

            var result = hashed1 == hashed1;

            Assert.IsTrue(result);
        }

        [Test]
        public void Equality_WhenSameUnderlyingTarget_ShouldReturnTrue()
        {
            var targetString = "asdf";
            var hashed1 = new HashedString(targetString);
            var hashed2 = new HashedString(targetString);

            var result = hashed1 == hashed2;

            Assert.IsTrue(result);
        }

        [Test]
        public void Inequality_WhenSameUnderlyingTarget_ShouldReturnFalse()
        {
            var targetString = "asdf";
            var hashed1 = new HashedString(targetString);
            var hashed2 = new HashedString(targetString);

            var result = hashed1 != hashed2;

            Assert.IsFalse(result);
        }

        [Test]
        public void Inequality_WhenDifferentUnderlyingTarget_ShouldReturnTrue()
        {
            var hashed1 = new HashedString("asdf");
            var hashed2 = new HashedString("asdf2");

            var result = hashed1 != hashed2;

            Assert.IsTrue(result);
        }
    }
}