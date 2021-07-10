using System;
using System.Collections;
using NUnit.Framework;
using ULox;
using ULox.Tests;
using System.Linq;
using System.IO;

public class UloxScripts
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
    public void NoFailTests(string file)
    {
        var script = File.ReadAllText(file);
        engine.Run(script);

        Assert.IsTrue(engine.AllPassed);
        Assert.AreNotEqual(0, engine.TestsFound, "Expect to find at least 1 test in the NoFail tests folder");
    }

    public static IEnumerator ScriptGenerator()
    {
        const string FolderName = @"Assets\ulox\Tests\uLoxTestScripts\NoFail";
        var filesInFolder = Directory.GetFiles(FolderName, "*.txt");

        return filesInFolder
            .Select(x => new object[] { x })
            .GetEnumerator();
    }
}
