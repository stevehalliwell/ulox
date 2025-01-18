using System;
using System.Collections.Generic;
using System.IO;
using static ULox.Chunk;

namespace ULox
{
    public static class CompiledScriptSerialisation
    {
        public const int Version = 1;

        //We are doing this ourselves to avoid need to put serialisable on everything under the sun

        public static byte[] Serialise(CompiledScript compiledScript)
        {
            using var stream = new MemoryStream();
            using var binaryWriter = new BinaryWriter(stream);

            binaryWriter.Write(Version);
            binaryWriter.Write(compiledScript.ScriptHash);
            binaryWriter.Write(compiledScript.AllChunks.Count);

            foreach (var chunk in compiledScript.AllChunks)
            {
                binaryWriter.Write(chunk.ChunkName);
                binaryWriter.Write(chunk.SourceName);
                binaryWriter.Write(chunk.ContainingChunkChainName);
                binaryWriter.Write(chunk.UpvalueCount);
                binaryWriter.Write(chunk.instructionCount);

                binaryWriter.Write(chunk.Constants.Count);
                foreach (var constant in chunk.Constants)
                {
                    binaryWriter.Write((byte)constant.type);
                    switch (constant.type)
                    {
                    case ValueType.Double:
                        binaryWriter.Write(constant.val.asDouble);
                        break;
                    case ValueType.String:
                        binaryWriter.Write(constant.val.asString.String);
                        break;
                    case ValueType.Chunk:
                        binaryWriter.Write(constant.val.asChunk.ChunkName);
                        break;
                    }
                }

                binaryWriter.Write(chunk.RunLengthLineNumbers.Count);
                foreach (var runLengthLineNumber in chunk.RunLengthLineNumbers)
                {
                    binaryWriter.Write(runLengthLineNumber.startingInstruction);
                    binaryWriter.Write(runLengthLineNumber.line);
                }

                binaryWriter.Write(chunk.Labels.Count);
                foreach (var pair in chunk.Labels)
                {
                    binaryWriter.Write(pair.Key.id);
                    binaryWriter.Write(pair.Value);
                }

                binaryWriter.Write(chunk.LabelNames.Count);
                foreach (var pair in chunk.LabelNames)
                {
                    binaryWriter.Write(pair.Key.id);
                    binaryWriter.Write(pair.Value.String);
                }

                binaryWriter.Write(chunk.Instructions.Count);
                foreach (var instruction in chunk.Instructions)
                {
                    binaryWriter.Write(instruction._data);
                }

                binaryWriter.Write(chunk.ArgumentConstantIds.Count);
                foreach (var argumentConstantId in chunk.ArgumentConstantIds)
                {
                    binaryWriter.Write(argumentConstantId);
                }

                binaryWriter.Write(chunk.ReturnConstantIds.Count);
                foreach (var returnConstantId in chunk.ReturnConstantIds)
                {
                    binaryWriter.Write(returnConstantId);
                }
            }

            binaryWriter.Write(compiledScript.CompilerMessages.Count);
            foreach (var compilerMessage in compiledScript.CompilerMessages)
            {
                binaryWriter.Write(compilerMessage.Message);
            }

            binaryWriter.Flush();
            return stream.ToArray();
        }

        public static CompiledScript Deserialise(byte[] bytes)
        {
            var lateChunkConstants = new List<(Chunk, int, string)>();
            using var stream = new MemoryStream(bytes);
            using var binaryReader = new BinaryReader(stream);

            var version = binaryReader.ReadInt32();
            if (version != Version)
                throw new InvalidOperationException($"Expected version '{Version}' but found '{version}'.");

            var scriptHash = binaryReader.ReadInt32();
            var chunkCount = binaryReader.ReadInt32();

            var compiledScript = new CompiledScript(scriptHash);

            for (int i = 0; i < chunkCount; i++)
            {
                var chunk = new Chunk(
                    binaryReader.ReadString(),
                    binaryReader.ReadString(),
                    binaryReader.ReadString())
                {
                    UpvalueCount = binaryReader.ReadInt32(),
                    instructionCount = binaryReader.ReadInt32()
                };

                var constantCount = binaryReader.ReadInt32();
                for (int j = 0; j < constantCount; j++)
                {
                    var type = (ValueType)binaryReader.ReadByte();
                    switch (type)
                    {
                    case ValueType.Double:
                        chunk.Constants.Add(Value.New(binaryReader.ReadDouble()));
                        break;
                    case ValueType.String:
                        chunk.Constants.Add(Value.New(binaryReader.ReadString()));
                        break;
                    case ValueType.Chunk:
                        //todo this should mark the info required to wire this up once all are processed
                        lateChunkConstants.Add((chunk, chunk.Constants.Count, binaryReader.ReadString()));
                        chunk.Constants.Add(Value.Null());
                        break;
                    }
                }

                var runLengthLineNumberCount = binaryReader.ReadInt32();
                for (int j = 0; j < runLengthLineNumberCount; j++)
                {
                    chunk.RunLengthLineNumbers.Add(new RunLengthLineNumber
                    {
                        startingInstruction = binaryReader.ReadInt32(),
                        line = binaryReader.ReadInt32()
                    });
                }

                var labelCount = binaryReader.ReadInt32();
                for (int j = 0; j < labelCount; j++)
                {
                    chunk.Labels.Add(new(binaryReader.ReadUInt16()), binaryReader.ReadInt32());
                }

                var labelNameCount = binaryReader.ReadInt32();
                for (int j = 0; j < labelNameCount; j++)
                {
                    chunk.LabelNames.Add(new(binaryReader.ReadUInt16()), new(binaryReader.ReadString()));
                }

                var instructionCount = binaryReader.ReadInt32();
                for (int j = 0; j < instructionCount; j++)
                {
                    chunk.Instructions.Add(ByteCodePacket.FromUint(binaryReader.ReadUInt32()));
                }

                var argumentConstantIdCount = binaryReader.ReadInt32();
                for (int j = 0; j < argumentConstantIdCount; j++)
                {
                    chunk.ArgumentConstantIds.Add(binaryReader.ReadByte());
                }

                var returnConstantIdCount = binaryReader.ReadInt32();
                for (int j = 0; j < returnConstantIdCount; j++)
                {
                    chunk.ReturnConstantIds.Add(binaryReader.ReadByte());
                }

                compiledScript.AllChunks.Add(chunk);
            }

            foreach (var (chunk, index, chunkName) in lateChunkConstants)
            {
                chunk.Constants[index] = Value.New(compiledScript.AllChunks.Find(x => x.ChunkName == chunkName));
            }

            return compiledScript;
        }
    }
}
