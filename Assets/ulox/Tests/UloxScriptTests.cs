using NUnit.Framework;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using ULox;
using ULox.Tests;

public class UloxScriptTestBase
{
    private const string ULoxScriptExtension = "*.ulox";

    public class ScriptTestEngine : ByteCodeInterpreterTestEngine
    {
        public ScriptTestEngine(Action<string> logger) : base(logger)
        {
        }

        public bool AllPassed => Vm.TestRunner.AllPassed;
        public int TestsFound => Vm.TestRunner.TestsFound;
    }

    public static string[] GetFilesInFolder(string FolderName)
    {
        string[] filesInFolder = new string[] { "" };

        var path = Path.GetFullPath(FolderName);
        if (Directory.Exists(path))
        {
            var foundInDir = Directory.GetFiles(path, ULoxScriptExtension);

            if (foundInDir.Any())
                filesInFolder = foundInDir;
        }

        return filesInFolder;
    }

    protected static TestCaseData MakeTestCaseData(string file)
    {
        if (!File.Exists(file))
            return new TestCaseData(new object[] { "" }).SetName("Empty");

        return new TestCaseData(new object[] { File.ReadAllText(file) }).SetName(Path.GetFileName(file));
    }

    protected ScriptTestEngine engine;

    [SetUp]
    public void Setup()
    {
        engine = new ScriptTestEngine(UnityEngine.Debug.Log);
        engine.AddLibrary(new AssertLibrary(() => new Vm()));
        engine.Engine.Context.DeclareLibrary(new DebugLibrary());
        engine.Engine.Context.DeclareLibrary(new StandardClassesLibrary());
        engine.Engine.Context.DeclareLibrary(new VmLibrary(() => new Vm()));
        engine.Engine.Context.DeclareLibrary(new DiLibrary());
    }

}

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
        string[] filesInFolder = GetFilesInFolder(NoFailFolderName);

        return filesInFolder
            .Select(x => MakeTestCaseData(x))
            .GetEnumerator();
    }
}


public class TryItOutTests : UloxScriptTestBase
{
    public const string TryItOutFolderName = @"Assets\ulox\Tests\uLoxTestScripts\TryItOutSamples";

    [Test]
    [TestCaseSource(nameof(ScriptGenerator))]
    public void NoFailTests(string script)
    {
        if (string.IsNullOrEmpty(script))
            return;

        engine.Run(script);
    }

    public static IEnumerator ScriptGenerator()
    {
        string[] filesInFolder = GetFilesInFolder(TryItOutFolderName);

        return filesInFolder
            .Select(x => MakeTestCaseData(x))
            .GetEnumerator();
    }
}
