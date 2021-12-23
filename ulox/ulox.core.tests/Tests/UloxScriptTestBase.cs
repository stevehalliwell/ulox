﻿using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using ULox;
using ULox.Tests;

public class UloxScriptTestBase
{
    private const string ULoxScriptExtension = "*.ulox";

    protected internal class ScriptTestEngine : ByteCodeInterpreterTestEngine
    {
        public ScriptTestEngine(Action<string> logger) : base(logger)
        {
        }

        public bool AllPassed => Vm.TestRunner.AllPassed;
        public int TestsFound => Vm.TestRunner.TestsFound;
    }

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
    {
        //if (!File.Exists(file))
        //    return new TestCaseData(new object[] { "" }).SetName($"NoFile: {file}");

        return new TestCaseData(new object[] { File.ReadAllText(file) }).SetName(Path.GetFileName(file));
    }

    protected ScriptTestEngine engine;

    [SetUp]
    public virtual void Setup()
    {
        engine = new ScriptTestEngine(System.Console.WriteLine);
        engine.AddLibrary(new AssertLibrary(() => new Vm()));
        engine.Engine.Context.DeclareLibrary(new DebugLibrary());
        engine.Engine.Context.DeclareLibrary(new StandardClassesLibrary());
        engine.Engine.Context.DeclareLibrary(new VmLibrary(() => new Vm()));
        engine.Engine.Context.DeclareLibrary(new DiLibrary());
        engine.Engine.Context.DeclareLibrary(new FreezeLibrary());
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
    {
        return TestContext.CurrentContext.TestDirectory;
    }
}
