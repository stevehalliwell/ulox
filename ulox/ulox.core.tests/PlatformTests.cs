using System;
using NUnit.Framework;

namespace ULox.Core.Tests
{    
    [TestFixture]
    public class PlatformTests
    {
        [Test]
        public void ctor_WhenDefault_ShouldNotThrow()
        {
            
            void Act() { new DirectoryLimitedPlatform(new(Environment.CurrentDirectory)); }

            Assert.DoesNotThrow(Act);
        }
        
        [Test]
        public void FindFiles_WhenRootAndAllAndFalse_ShouldReturnNonEmptyArray()
        {
            var plat = new DirectoryLimitedPlatform(new(Environment.CurrentDirectory));

            var res = plat.FindFiles("./", "*", false);

            Assert.IsTrue(res.Length > 0);
        }
    }
}