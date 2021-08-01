using System.Collections.Generic;

namespace ULox
{
    public class TestDeclarationCompilette : ICompilette
    {
        private readonly ClassCompilette _classCompilette;
        private readonly List<ushort> _currentTestcaseInstructions = new List<ushort>(); 

        public TestDeclarationCompilette(ClassCompilette classCompilette)
        {
            _classCompilette = classCompilette;
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

            //parse as class, class needs to add calls for all testcases it finds to the testFuncChunk
            _classCompilette.Process(compiler);

            var testcaseCount = _currentTestcaseInstructions.Count;
            if (testcaseCount > byte.MaxValue)
                throw new VMException($"{testcaseCount} has more than {byte.MaxValue} testcases, this is not allowed.");

            compiler.EmitOpAndBytes(OpCode.TEST, (byte)TestOpType.TestSetStart, testSetNameID, (byte)testcaseCount);

            for (int i = 0; i < _currentTestcaseInstructions.Count; i++)
            {
                compiler.EmitUShort(_currentTestcaseInstructions[i]);
            }

            _currentTestcaseInstructions.Clear();

            compiler.EmitOpCode(OpCode.NULL);
            compiler.EmitOpAndBytes(OpCode.ASSIGN_GLOBAL, compiler.CurrentChunk.AddConstant(Value.New(testClassName)));
            compiler.EmitOpAndBytes(OpCode.TEST, (byte)TestOpType.TestSetEnd, 0, 0);

            CurrentTestSetName = null;
        }

        internal void AddTestCaseInstruction(ushort currentChunkInstructinCount)
        {
            _currentTestcaseInstructions.Add(currentChunkInstructinCount);
        }
    }
}
