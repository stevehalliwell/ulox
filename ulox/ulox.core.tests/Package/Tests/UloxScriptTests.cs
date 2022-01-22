using NUnit.Framework;

public class NoFailUloxTests : UloxScriptTestBase
{
    public const string NoFailFolderName = "Package/Scripts/Tests";

    [TestCaseSource(nameof(ScriptGenerator))]
    public void Tests(string script)
    {
        if (string.IsNullOrEmpty(script))
            return;

        engine.Run(script);

        Assert.IsTrue(engine.MyEngine.Context.VM.TestRunner.AllPassed);
        Assert.AreNotEqual(0, engine.MyEngine.Context.VM.TestRunner.TestsFound, "Expect to find at least 1 test in the NoFail tests folder");
    }

    public static TestCaseData[] ScriptGenerator()
    {
        return ScriptGeneratorHelper(NoFailFolderName);
    }
}
