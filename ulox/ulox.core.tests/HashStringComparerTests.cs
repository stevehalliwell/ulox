using NUnit.Framework;
using System.Collections.Generic;

namespace ULox.Core.Tests
{
    [TestFixture]
    public class HashStringComparerTests
    {
        [Test]
        public void Compare_WhenDictDoesContain_ShouldReturnTrue()
        {
            var doesExist = new HashedString("doesExist");
            var dict = new Dictionary<HashedString, object>(new HashedStringComparer());
            dict[doesExist] = new object();

            var shouldExist = dict.TryGetValue(doesExist, out var _);

            Assert.IsTrue(shouldExist);
        }

        [Test]
        public void CompareTo_WhenSameValues_ShouldReturnTrue()
        {
            var doesExist = new HashedString("doesExist");
            var doesExist2 = new HashedString("doesExist");

            var compareRes = doesExist.CompareTo(doesExist2);

            Assert.AreEqual(0, compareRes);
        }

        [Test]
        public void Compare_WhenDictDoesNotContain_ShouldReturnFalse()
        {
            var doesNotExist = new HashedString("doesNotExist");
            var doesExist = new HashedString("doesExist");
            var dict = new Dictionary<HashedString, object>(new HashedStringComparer());
            dict[doesExist] = new object();

            var shouldNotExistResult = dict.TryGetValue(doesNotExist, out var _);

            Assert.IsFalse(shouldNotExistResult);
        }
    }
}