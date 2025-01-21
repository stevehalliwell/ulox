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
print(res.Count());
print(res);");
            
            StringAssert.DoesNotStartWith("0", testEngine.InterpreterResult);
            StringAssert.Contains("<inst NativeList>", testEngine.InterpreterResult);
        }

        [Test]
        public void PlatformWriteTempFile_ReadTempFile_ShouldMatch()
        {
            testEngine.Run(@"
var fileContent = ""hello"";
var fileLoc = ""temp:PlatformWriteTempFile_ReadTempFile_ShouldMatch.txt"";
Platform.WriteFile(fileLoc, fileContent);
var readBack = Platform.ReadFile(fileLoc);
print(fileContent == readBack)");

            Assert.AreEqual("True", testEngine.InterpreterResult);
        }
    }
}