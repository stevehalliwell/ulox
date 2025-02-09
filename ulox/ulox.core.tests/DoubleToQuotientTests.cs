using System;
using NUnit.Framework;

namespace ULox.Core.Tests
{
    [TestFixture]
    public class DoubleToQuotientTests
    {
        [Test]
        [TestCase(0.0, true, 0, 1u)]
        [TestCase(1.0, true, 1, 1u)]
        [TestCase(2.0, true, 2, 1u)]
        [TestCase(0.5, true, 1, 2u)]
        [TestCase(0.25, true, 1, 4u)]
        [TestCase(0.75, true, 3, 4u)]
        [TestCase(-0.75, true, -3, 4u)]
        [TestCase(0.3333333333333333, true, 1, 3u)]
        [TestCase(0.518518, true, 37031, 71417u)]
        [TestCase(0.518518518518, true, 14, 27u)]
        [TestCase(0.9054054054054, true, 67, 74u)]
        public void Whole_WhenToQuotient_1Denom(
            double test,
            bool expectedPos,
            int expectedNume,
            uint expectedDenom)
        {
            var (isPossible, nume, denom) = DoubleToQuotient.ToQuotient(test, 10);//we care about best byte div so 8 is 1/256

            Console.WriteLine($"expected:{expectedNume}/{expectedDenom} ({expectedNume / (double)expectedDenom})");
            Console.WriteLine($"actual:{nume}/{(double)denom} ({nume/(double)denom})");
            Console.WriteLine(test);
            Console.WriteLine(nume / (double)denom);
            Assert.Multiple(() =>
            {
                Assert.AreEqual(expectedPos, isPossible);
                Assert.AreEqual(expectedNume, nume);
                Assert.AreEqual(expectedDenom, denom);
            });
        }
    }
}