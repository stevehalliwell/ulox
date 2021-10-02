using NUnit.Framework;
using System.Collections;
using System.Linq;

[TestFixture]
public class TryItOutTests : UloxScriptTestBase
{
    public const string TryItOutFolderName = @"Assets\ulox\Tests\uLoxTestScripts\TryItOutSamples";

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        engine.ReThrow = true;
    }

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
        return ScriptGeneratorHelper(TryItOutFolderName);
    }
}
