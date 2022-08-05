using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using ULox;
using ULox.Tests;

public class UloxScriptTestBase
{
    private const string ULoxScriptExtension = "*.ulox";

    public static string[] GetFilesInSubFolder(string subFolderName)
    {
        var folderName = Path.Combine(UloxTestFolder(), subFolderName);
        string[] filesInFolder = new string[] { folderName };

        var path = Path.GetFullPath(folderName);
        if (Directory.Exists(path))
        {
            var foundInDir = Directory.GetFiles(path, ULoxScriptExtension);

            if (foundInDir.Any())
                filesInFolder = foundInDir;
        }

        return filesInFolder;
    }

    protected static TestCaseData MakeTestCaseData(string file) 
        => new TestCaseData(new object[] { File.ReadAllText(file) }).SetName(Path.GetFileName(file));

    protected ByteCodeInterpreterTestEngine engine;

    [SetUp]
    public virtual void Setup()
    {
        engine = new ByteCodeInterpreterTestEngine(Console.WriteLine);
        engine.MyEngine.Context.DeclareLibrary(new DebugLibrary());
        engine.MyEngine.Context.DeclareLibrary(new VmLibrary(() => new Vm()));
        engine.MyEngine.Context.DeclareLibrary(new DiLibrary());
        engine.MyEngine.Context.DeclareLibrary(new FreezeLibrary());
    }

    protected static TestCaseData[] ScriptGeneratorHelper(string subfolderName)
    {
        string[] filesInFolder = GetFilesInSubFolder(subfolderName);

        if (filesInFolder == null
            || filesInFolder.Length == 0)
            return new TestCaseData[]
            {
                new TestCaseData("").SetName($"No test files found in {subfolderName}")
            };
        
        return filesInFolder
            .Select(x => MakeTestCaseData(x))
            .ToArray();
    }

    public static string UloxTestFolder()
        => TestContext.CurrentContext.TestDirectory;
}
