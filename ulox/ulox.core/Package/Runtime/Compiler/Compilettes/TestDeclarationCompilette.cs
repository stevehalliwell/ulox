using System.Collections.Generic;

namespace ULox
{
    public class TestDeclarationCompilette : ICompilette
    {
        private readonly List<ushort> _currentTestcaseInstructions = new List<ushort>();

        public TokenType Match 
            => TokenType.TEST;

        public string CurrentTestSetName { get; internal set; }

        public void Process(CompilerBase compiler)
        {
            TestDeclaration(compiler);
        }

        private void TestDeclaration(CompilerBase compiler)
        {
            //grab name
            var testClassName = (string)compiler.TokenIterator.CurrentToken.Literal;
            CurrentTestSetName = testClassName;
            var testSetNameID = compiler.CurrentChunk.AddConstant(Value.New(testClassName));
            compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect test set name.");

            compiler.TokenIterator.Consume(TokenType.OPEN_BRACE, "Expect '{' before test set body.");

            compiler.BlockStatement();

            var testcaseCount = _currentTestcaseInstructions.Count;
            if (testcaseCount > byte.MaxValue)
                throw new VMException($"{testcaseCount} has more than {byte.MaxValue} testcases, this is not allowed.");

            compiler.EmitOpAndBytes(OpCode.TEST, (byte)TestOpType.TestSetStart, testSetNameID, (byte)testcaseCount);

            for (int i = 0; i < _currentTestcaseInstructions.Count; i++)
            {
                compiler.EmitUShort(_currentTestcaseInstructions[i]);
            }

            _currentTestcaseInstructions.Clear();

            compiler.EmitOpAndBytes(OpCode.TEST, (byte)TestOpType.TestSetEnd, 0, 0);

            CurrentTestSetName = null;
        }

        internal void AddTestCaseInstruction(ushort currentChunkInstructinCount)
        {
            _currentTestcaseInstructions.Add(currentChunkInstructinCount);
        }

        public void TSName(CompilerBase compiler, bool obj)
        {
            var tsname = CurrentTestSetName;
            compiler.AddConstantAndWriteOp(Value.New(tsname));
        }
    }
}
