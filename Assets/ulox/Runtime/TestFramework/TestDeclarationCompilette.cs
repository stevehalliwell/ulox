using System.Collections.Generic;

namespace ULox
{
    public class TestDeclarationCompilette : ICompilette
    {
        private readonly List<ushort> _currentTestcaseInstructions = new List<ushort>();
        private readonly TestcaseCompillette _testcaseCompillette;

        public TestDeclarationCompilette(TestcaseCompillette testcaseCompillette)
        {
            _testcaseCompillette = testcaseCompillette;
            _testcaseCompillette.SetTestDeclarationCompilette(this);
        }

        public TokenType Match => TokenType.TEST;

        public string CurrentTestSetName { get; internal set; }

        public void Process(CompilerBase compiler)
        {
            TestDeclaration(compiler);
        }

        private void TestDeclaration(CompilerBase compiler)
        {
            //grab name
            var testClassName = (string)compiler.CurrentToken.Literal;
            CurrentTestSetName = testClassName;
            var testSetNameID = compiler.CurrentChunk.AddConstant(Value.New(testClassName));
            compiler.Consume(TokenType.IDENTIFIER, "Expect test set name.");


            compiler.Consume(TokenType.OPEN_BRACE, "Expect '{' before test set body.");
            while (!compiler.Check(TokenType.CLOSE_BRACE) && !compiler.Check(TokenType.EOF))
            {
                if (compiler.Match(TokenType.TESTCASE))
                    _testcaseCompillette.Process(compiler);
                else
                    throw new CompilerException($"{nameof(TestDeclarationCompilette)} encountered unexpected token '{compiler.CurrentToken.TokenType}'");
            }

            compiler.Consume(TokenType.CLOSE_BRACE, "Expect '}' after class body.");


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
    }
}
