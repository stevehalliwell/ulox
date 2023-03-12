using NUnit.Framework;
using ULox;

namespace ULox.Core.Tests
{
    [TestFixture]
    public class TableTests
    {
        [Test]
        public void Add_WhenEmpty_ShouldHave1()
        {
            var table = new Table();
            var hs = new HashedString("test");

            table.Add(hs, Value.Null());

            Assert.AreEqual(1, table.Count);
        }

        [Test]
        public void Contains_When1Added_ShouldReturnTrue()
        {
            var table = new Table();
            var hsIn = new HashedString("test");
            var hsTest = new HashedString("test");

            table[hsIn] = Value.Null();
            table[hsTest] = Value.Null();

            Assert.AreEqual(1, table.Count);
        }
    }
}