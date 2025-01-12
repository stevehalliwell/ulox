using System.Collections.Generic;

namespace ULox
{
    public class TestSetDeclarationCompilette : ICompilette
    {
        private const string AnonTestSetPrefix = "Anon_TestSet_";
        private readonly List<byte> _currentTestcaseLabels = new();

        public TokenType MatchingToken
            => TokenType.TEST_SET;

        public string CurrentTestSetName { get; internal set; }

        public void Process(Compiler compiler)
        {
            TestDeclaration(compiler);
        }

        private void TestDeclaration(Compiler compiler)
        {
            //grab name
            var testClassName = compiler.IdentifierOrChunkUnique(AnonTestSetPrefix);
            CurrentTestSetName = testClassName;
            var testSetNameID = compiler.AddCustomStringConstant(testClassName);

            compiler.TokenIterator.Consume(TokenType.OPEN_BRACE, "Expect '{' before testset set body.");

            //testbody
            compiler.BeginScope();
            var labelID = compiler.GotoUniqueChunkLabel($"Test_{testClassName}");
            var testFixtureBodyLabel = compiler.LabelUniqueChunkLabel("TestFixtureBody");

            compiler.Block();
            compiler.EmitPacket(new ByteCodePacket(OpCode.YIELD));
            compiler.EndScope();
            compiler.EmitLabel(labelID);

            var testcaseCount = _currentTestcaseLabels.Count;
            if (testcaseCount > byte.MaxValue)
                compiler.ThrowCompilerException($"{testcaseCount} has more than {byte.MaxValue} tests, this is not allowed.");

            EmitTestPacket(compiler, TestOpType.TestFixtureBodyInstruction, testFixtureBodyLabel, 0);

            for (int i = 0; i < _currentTestcaseLabels.Count; i++)
            {
                EmitTestPacket(compiler, TestOpType.TestCase, _currentTestcaseLabels[i], testSetNameID);
            }

            _currentTestcaseLabels.Clear();
            EmitTestPacket(compiler, TestOpType.TestSetEnd, 0, 0);

            CurrentTestSetName = null;
        }

        public static void EmitTestPacket(Compiler compiler, TestOpType opType, byte b1, byte b2)
            => compiler.EmitPacket(new ByteCodePacket(OpCode.TEST, new ByteCodePacket.TestOpDetails(opType, b1, b2)));

        internal void AddTestCaseLabel(byte labelUniqueChunkLabel)
        {
            _currentTestcaseLabels.Add(labelUniqueChunkLabel);
        }

        public void TestSetName(Compiler compiler, bool obj)
        {
            var tsname = CurrentTestSetName;
            compiler.AddConstantStringAndWriteOp(tsname);
        }
    }
}
