using NUnit.Framework;

namespace ULox.Core.Tests
{
    public class PlatformEngineTests : EngineTestBase
    {
        [Test]
        public void PlatformFindFiles_Root_NotEmpty()
        {
            testEngine.Run(@"
var res = Platform.FindFiles(""./"",""*.ulox"",true);
print(res);
print(res.Count());
"
            );

            StringAssert.StartsWith("<inst NativeList>1", testEngine.InterpreterResult);
        }
    }
}