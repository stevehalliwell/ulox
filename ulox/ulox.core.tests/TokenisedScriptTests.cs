using NUnit.Framework;

namespace ULox.Core.Tests
{
    //todo TokenisedScript serialisation tests
    public class TokenisedScriptTests
    {
        [Test]
        public void Serialise_WhenRoundTrip_ShouldMatch()
        {
            var testString = @"var a = ""hello"";";
            var scanner = new Scanner();
            var tokenisedScript = scanner.Scan(new Script("test", testString));

            var serialised = TokenisedScriptSerialisation.Serialise(tokenisedScript);
            var deserialised = TokenisedScriptSerialisation.Deserialise(serialised);

            CollectionAssert.AreEqual(tokenisedScript.Tokens, deserialised.Tokens);
            CollectionAssert.AreEqual(tokenisedScript.LineLengths, deserialised.LineLengths);
        }
    }

    //todo types get added to vm as part of compile so how do we handle that
    public class CompiledScriptTests : EngineTestBase
    {
        public const string TestString = @"
var a = ""hello"";

print(a);
";

        [Test]
        public void Serialise_WhenRoundTrip_ShouldMatch()
        {
            var scanner = new Scanner();
            var tokenisedScript = scanner.Scan(new Script("test", TestString));
            var compiler = new Compiler();
            var compiledScript = compiler.Compile(tokenisedScript);

            var serialised = CompiledScriptSerialisation.Serialise(compiledScript);
            var deserialised = CompiledScriptSerialisation.Deserialise(serialised);

            Assert.AreEqual(compiledScript.ScriptHash, deserialised.ScriptHash);
            Assert.AreEqual(compiledScript.AllChunks.Count, deserialised.AllChunks.Count);
            for (int i = 0; i < compiledScript.AllChunks.Count; i++)
            {
                var lhs = compiledScript.AllChunks[i];
                var rhs = deserialised.AllChunks[i];
                CollectionAssert.AreEqual(lhs.Constants, rhs.Constants);
                CollectionAssert.AreEqual(lhs.RunLengthLineNumbers, rhs.RunLengthLineNumbers);
                CollectionAssert.AreEqual(lhs.Labels, rhs.Labels);
                CollectionAssert.AreEqual(lhs.Instructions, rhs.Instructions);
                CollectionAssert.AreEqual(lhs.ArgumentConstantIds, rhs.ArgumentConstantIds);
                CollectionAssert.AreEqual(lhs.ReturnConstantIds, rhs.ReturnConstantIds);
                Assert.AreEqual(lhs.ChunkName, rhs.ChunkName);
                Assert.AreEqual(lhs.SourceName, rhs.SourceName);
                Assert.AreEqual(lhs.ContainingChunkChainName, rhs.ContainingChunkChainName);
            }
            CollectionAssert.AreEqual(compiledScript.CompilerMessages, deserialised.CompilerMessages);
        }

        [Test]
        public void Run_WhenDeserialised_ShouldMatch()
        {
            var compiledScript = testEngine.MyEngine.Context.CompileScript(new Script("test", TestString));
            var serialised = CompiledScriptSerialisation.Serialise(compiledScript);
            var deserialised = CompiledScriptSerialisation.Deserialise(serialised);

            testEngine.MyEngine.Context.Vm.Interpret(deserialised.TopLevelChunk);

            Assert.AreEqual("hello", testEngine.InterpreterResult);
        }
    }

    //todo Cache tokenised script tests
    //when you hit the scriptlocator, it should be able to check for a cached
}