using NUnit.Framework;
using System.Collections;

public class NoFailUloxTests : UloxScriptTestBase
{
    public const string NoFailFolderName = @"Assets\ulox\Tests\uLoxTestScripts\NoFail";

    [Test]
    [TestCaseSource(nameof(ScriptGenerator))]
    public void NoFailTests(string script)
    {
        if (string.IsNullOrEmpty(script))
            return;

        engine.Run(script);

        Assert.IsTrue(engine.AllPassed);
        Assert.AreNotEqual(0, engine.TestsFound, "Expect to find at least 1 test in the NoFail tests folder");
    }

    public static IEnumerator ScriptGenerator()
    {
        return ScriptGeneratorHelper(NoFailFolderName);
    }
}
