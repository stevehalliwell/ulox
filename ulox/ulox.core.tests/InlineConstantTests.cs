using System.Linq;
using NUnit.Framework;

namespace ULox.Core.Tests
{
    [TestFixture]
    public class InlineConstantTests : EngineTestBase
    {
        [Test]
        public void Constants_WhenExpressableAsByteCodeValue_ShouldBeLimited()
        {
            testEngine.Run(@"
var a = 1;
a = 2;
a = true;
a = null;
a = 1/2;
a = 0.5;
a = 0.25;
a = 0.75;
a = 0.3333333333333333;
a = -0.8;
a = -5000.5;
a = 2000;
");

            Assert.AreEqual(
                1,
                testEngine.MyEngine.Context.Program.CompiledScripts.Sum(x => x.AllChunks.Sum(x => x.Constants.Count)) );
        }
    }
}