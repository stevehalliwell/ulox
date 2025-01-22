using System.Collections.Generic;

namespace ULox
{
    public class TestSetDeclarationCompilette : ICompilette
    {
        private const string AnonTestSetPrefix = "Anon_TestSet_";
        private readonly List<Label> _currentTestcaseLabels = new();

        public TokenType MatchingToken
            => TokenType.TEST_SET;

        public string CurrentTestSetName { get; internal set; }

        public void Process(Compiler compiler)
        {
            TestSetDeclaration(compiler);
        }

        private void TestSetDeclaration(Compiler compiler)
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

            //this is hard 0 on b4
            EmitTestPacket(compiler, new(TestOpType.TestSetName, testSetNameID,0));
            EmitTestPacket(compiler, new(TestOpType.TestSetBodyLabel, testFixtureBodyLabel));

            for (int i = 0; i < _currentTestcaseLabels.Count; i++)
            {
                //todo why do we need two names in here? if we didnt we could put ushort label id in here
                EmitTestPacket(compiler, new(TestOpType.TestCase, _currentTestcaseLabels[i]));
            }

            _currentTestcaseLabels.Clear();
            EmitTestPacket(compiler, new(TestOpType.TestSetEnd, Label.Default));

            CurrentTestSetName = null;
        }

        public static void EmitTestPacket(Compiler compiler, ByteCodePacket.TestOpDetails testOpDetails)
            => compiler.EmitPacket(new ByteCodePacket(OpCode.TEST, testOpDetails));

        internal void AddTestCaseLabel(Label labelUniqueChunkLabel)
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
