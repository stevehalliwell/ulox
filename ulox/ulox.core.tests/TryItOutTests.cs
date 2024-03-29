﻿using NUnit.Framework;

[TestFixture]
public class TryItOutTests : UloxScriptTestBase
{
    public const string TryItOutFolderName = "uloxs/Samples";

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        engine.ReThrow = true;
    }

    [TestCaseSource(nameof(ScriptGenerator))]
    public void Tests(string script)
    {
        if (string.IsNullOrEmpty(script))
            return;

        engine.Run(script);
    }

    public static TestCaseData[] ScriptGenerator()
    {
        return ScriptGeneratorHelper(TryItOutFolderName);
    }
}
