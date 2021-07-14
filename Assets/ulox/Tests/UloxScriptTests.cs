using System;
using System.Collections;
using NUnit.Framework;
using ULox;
using ULox.Tests;
using System.Linq;
using System.IO;

public class UloxScriptTests
{
    public class ScriptTestEngine : ByteCodeInterpreterTestEngine
    {
        public ScriptTestEngine(Action<string> logger) : base(logger)
        {
        }

        public bool AllPassed => VM.TestRunner.AllPassed;
        public int TestsFound => VM.TestRunner.TestsFound;
    }

    private ScriptTestEngine engine;

    [SetUp]
    public void Setup()
    {
        engine = new ScriptTestEngine(UnityEngine.Debug.Log);
        engine.AddLibrary(new AssertLibrary());
    }

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
        const string FolderName = @"Assets\ulox\Tests\uLoxTestScripts\NoFail";

        string[] filesInFolder = new string[] { "" };

        var path = Path.GetFullPath(FolderName);
        if (Directory.Exists(path))
        {
            var foundInDir = Directory.GetFiles(path, "*.txt");

            if (foundInDir.Any())
                filesInFolder = foundInDir;
        }

        return filesInFolder
            .Select(x => MakeTestCaseData(x))
            .GetEnumerator();
    }

    private static TestCaseData MakeTestCaseData(string file)
    {
        if (!File.Exists(file))
            return new TestCaseData(new object[] { "" }).SetName("Empty");

        return new TestCaseData(new object[] { File.ReadAllText(file) }).SetName(Path.GetFileName(file));
    }
}
