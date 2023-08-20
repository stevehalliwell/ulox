using NUnit.Framework;

namespace ULox.Core.Tests
{
    [TestFixture]
    public class FastListTests
    {
        [Test]
        public void Count_WhenEmpty_ShouldHave0()
        {
            var fastList = new FastList<object>();

            Assert.AreEqual(0, fastList.Count);
        }

        [Test]
        public void Add_WhenCalledOnEmpty_ShouldHave1Count()
        {
            var fastList = new FastList<object>();

            fastList.Add(new object());

            Assert.AreEqual(1, fastList.Count);
        }

        [Test]
        public void RemoveAt_WhenCalledOnOneElement_ShouldHave0Count()
        {
            var fastList = new FastList<object>();
            fastList.Add(new object());

            fastList.RemoveAt(0);

            Assert.AreEqual(0, fastList.Count);
        }

        [Test]
        public void Remove_WhenHasElement_ShouldHave0Count()
        {
            var fastList = new FastList<object>();
            var obj = new object();
            fastList.Add(obj);

            fastList.Remove(obj);

            Assert.AreEqual(0, fastList.Count);
        }

        [Test]
        public void Clear_WhenCalledOnOneElement_ShouldHave0Count()
        {
            var fastList = new FastList<object>();
            fastList.Add(new object());

            fastList.Clear();

            Assert.AreEqual(0, fastList.Count);
        }

        [Test]
        public void Indexer_WhenCalledOnOneElement_ShouldReturnElement()
        {
            var fastList = new FastList<object>();
            var obj = new object();
            fastList.Add(obj);

            var result = fastList[0];

            Assert.AreEqual(obj, result);
        }

        [Test]
        public void Indexer_WhenCalledOnOneElement_ShouldSetElement()
        {
            var fastList = new FastList<object>();
            var obj = new object();
            fastList.Add(obj);

            fastList[0] = new object();

            Assert.AreNotEqual(obj, fastList[0]);
        }

        [Test]
        public void GetEnumerator_WhenTwoElements_ShouldPrintBoth()
        {
            var fastList = new FastList<object>();
            fastList.Add(1);
            fastList.Add(2);
            var res = "";

            foreach (var item in fastList)
            {
                res += item.ToString();
            }

            StringAssert.AreEqualIgnoringCase("12", res);
        }
    }
}