using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ULox
{
    public class TestDeclarationCompilette : ICompilette
    {
        private readonly List<byte> _currentTestcaseLabels = new List<byte>();

        public TokenType Match
            => TokenType.TEST;

        public string CurrentTestSetName { get; internal set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Process(Compiler compiler)
        {
            TestDeclaration(compiler);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TestDeclaration(Compiler compiler)
        {
            //grab name
            var testClassName = (string)compiler.TokenIterator.CurrentToken.Literal;
            CurrentTestSetName = testClassName;
            var testSetNameID = compiler.CurrentChunk.AddConstant(Value.New(testClassName));
            compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect test set name.");

            compiler.TokenIterator.Consume(TokenType.OPEN_BRACE, "Expect '{' before test set body.");

            //testbody
            compiler.BeginScope();
            var labelID = compiler.GotoUniqueChunkLabel($"Test_{testClassName}");
            var testFixtureBodyLabel = compiler.LabelUniqueChunkLabel("TestFixtureBody");

            compiler.Block();
            compiler.EmitOpCode(OpCode.YIELD);
            compiler.EndScope();
            compiler.EmitLabel(labelID);

            var testcaseCount = _currentTestcaseLabels.Count;
            if (testcaseCount > byte.MaxValue)
                compiler.ThrowCompilerException($"{testcaseCount} has more than {byte.MaxValue} testcases, this is not allowed.");

            compiler.EmitOpAndBytes(OpCode.TEST, (byte)TestOpType.TestFixtureBodyInstruction, testFixtureBodyLabel);
            compiler.EmitOpAndBytes(OpCode.TEST, (byte)TestOpType.TestSetStart, testSetNameID, (byte)testcaseCount);

            for (int i = 0; i < _currentTestcaseLabels.Count; i++)
            {
                compiler.EmitBytes(_currentTestcaseLabels[i]);
            }

            _currentTestcaseLabels.Clear();
            compiler.EmitOpAndBytes(OpCode.TEST, (byte)TestOpType.TestSetEnd, 0, 0);

            CurrentTestSetName = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddTestCaseLabel(byte labelUniqueChunkLabel)
        {
            _currentTestcaseLabels.Add(labelUniqueChunkLabel);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TSName(Compiler compiler, bool obj)
        {
            var tsname = CurrentTestSetName;
            compiler.AddConstantAndWriteOp(Value.New(tsname));
        }
    }
}
