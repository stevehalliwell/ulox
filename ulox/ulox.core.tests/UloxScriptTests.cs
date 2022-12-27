using NUnit.Framework;

public class NoFailUloxTests : UloxScriptTestBase
{
    public const string NoFailFolderName = "uloxs/Tests";

    [TestCaseSource(nameof(ScriptGenerator))]
    public void Tests(string script)
    {
        if (string.IsNullOrEmpty(script))
            return;

        engine.Run(script);

        Assert.IsTrue(engine.MyEngine.Context.Vm.TestRunner.AllPassed);
        Assert.AreNotEqual(0, engine.MyEngine.Context.Vm.TestRunner.TestsFound, "Expect to find at least 1 test in the NoFail tests folder");
    }

    public static TestCaseData[] ScriptGenerator()
    {
        return ScriptGeneratorHelper(NoFailFolderName);
    }
}
