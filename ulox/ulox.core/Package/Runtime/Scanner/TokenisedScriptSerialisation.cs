using System.Collections.Generic;
using System.IO;

namespace ULox
{
    public static class TokenisedScriptSerialisation
    {
        public const int Version = 1;

        //We are doing this ourselves to avoid need to put serialisable on everything under the sun

        public static byte[] Serialise(TokenisedScript tokenisedScript)
        {
            var stream = new MemoryStream();
            var binaryWriter = new BinaryWriter(stream);

            binaryWriter.Write(Version);
            binaryWriter.Write(tokenisedScript.Tokens.Count);
            foreach (var token in tokenisedScript.Tokens)
            {
                binaryWriter.Write((byte)token.TokenType);
                binaryWriter.Write(token.Literal ?? string.Empty);
                binaryWriter.Write(token.StringSourceIndex);
            }

            binaryWriter.Write(tokenisedScript.LineLengths.Length);
            foreach (var lineLength in tokenisedScript.LineLengths)
            {
                binaryWriter.Write(lineLength);
            }

            binaryWriter.Flush();
            return stream.ToArray();
        }

        public static TokenisedScript Deserialise(byte[] data)
        {
            var stream = new MemoryStream(data);
            var binaryReader = new BinaryReader(stream);

            var version = binaryReader.ReadInt32();
            if (version != Version)
            {
                throw new System.Exception($"TokenisedScript Version mismatch, expected {Version} but got {version}");
            }

            var tokenCount = binaryReader.ReadInt32();
            var tokens = new List<Token>(tokenCount);
            for (int i = 0; i < tokenCount; i++)
            {
                var tokenType = (TokenType)binaryReader.ReadByte();
                var literal = binaryReader.ReadString();
                var stringSourceIndex = binaryReader.ReadInt32();
                tokens.Add(new Token(tokenType, literal == string.Empty ? null : literal, stringSourceIndex));
            }

            var lineLengthsCount = binaryReader.ReadInt32();
            var lineLengths = new int[lineLengthsCount];
            for (int i = 0; i < lineLengthsCount; i++)
            {
                lineLengths[i] = binaryReader.ReadInt32();
            }

            return new TokenisedScript
            {
                Tokens = tokens,
                LineLengths = lineLengths
            };
        }
    }
}
