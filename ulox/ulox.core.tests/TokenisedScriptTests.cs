﻿using NUnit.Framework;

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


    //todo Cache tokenised script tests
    //when you hit the scriptlocator, it should be able to check for a cached
}