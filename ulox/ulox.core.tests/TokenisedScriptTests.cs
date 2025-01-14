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

    //todo CompiledScript serialisation tests
    //todo chunk serialisation tests, the issue there is that constants can be non-trivial. They are always either number, string, or chunk

    public class CompiledScriptTests
    {
        [Test]
        public void DeepClone_WhenRoundTrip_ShouldMatch()
        {
            var testString = @"var a = ""hello"";";
            var scanner = new Scanner();
            var tokenisedScript = scanner.Scan(new Script("test", testString));
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
    }

    //todo Cache tokenised script tests
    //when you hit the scriptlocator, it should be able to check for a cached
}