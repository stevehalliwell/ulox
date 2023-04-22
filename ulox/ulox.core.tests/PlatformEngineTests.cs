using NUnit.Framework;

namespace ULox.Core.Tests
{
    public class PlatformEngineTests : EngineTestBase
    {
        [Test]
        public void PlatformFindFiles_Root_NotEmpty()
        {
            var expectedStart = "<inst NativeList>";
            testEngine.Run(@"
var res = Platform.FindFiles(""./"",""*.ulox"",true);
print(res);
print(res.Count());
"
            );
            
            StringAssert.StartsWith(expectedStart, testEngine.InterpreterResult);
            var countPrinted = int.Parse(testEngine.InterpreterResult.Substring(expectedStart.Length));
            Assert.AreNotEqual(0, countPrinted);
        }
    }
}