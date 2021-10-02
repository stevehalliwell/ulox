using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using ULox;
using ULox.Tests;
using System.Collections;

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
            return new TestCaseData(new object[] { "" }).SetName($"NoFile: {file}");

        return new TestCaseData(new object[] { File.ReadAllText(file) }).SetName(Path.GetFileName(file));
    }

    protected ScriptTestEngine engine;

    [SetUp]
    public virtual void Setup()
    {
        engine = new ScriptTestEngine(UnityEngine.Debug.Log);
        engine.AddLibrary(new AssertLibrary(() => new Vm()));
        engine.Engine.Context.DeclareLibrary(new DebugLibrary());
        engine.Engine.Context.DeclareLibrary(new StandardClassesLibrary());
        engine.Engine.Context.DeclareLibrary(new VmLibrary(() => new Vm()));
        engine.Engine.Context.DeclareLibrary(new DiLibrary());
    }

    protected static IEnumerator ScriptGeneratorHelper(string folderName)
    {
        string[] filesInFolder = GetFilesInFolder(folderName);

        if (filesInFolder == null ||
            filesInFolder.Length == 0)
            filesInFolder = new string[] { folderName };

        return filesInFolder
            .Select(x => MakeTestCaseData(x))
            .GetEnumerator();
    }

}
