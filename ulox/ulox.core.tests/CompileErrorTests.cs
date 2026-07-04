using NUnit.Framework;

namespace ULox.Core.Tests
{
    [TestFixture]
    public class CompileErrorTests : EngineTestBase
    {
        [Test]
        public void Var_MissingAssignOrSemi_Error()
        {
            testEngine.Run(@"
//var with missing assign followed by blocks should error
var foo
{
    [1,2,3,4,5,6,],
};  
");

            StringAssert.StartsWith("Expect ; after VarDeclaration", testEngine.InterpreterResult);
        }

        [Test]
        public void Error_WhenAfterBlankLinesAndComments_ShouldReportCorrectLine()
        {
            testEngine.Run(@"
// pad 1

// pad 3
class T
{
    init(){return 1;}
}");

            Assert.AreEqual("Cannot return an expression from an 'init' in chunk 'init(test)' at 7:18.", testEngine.InterpreterResult);
        }
    }
}
