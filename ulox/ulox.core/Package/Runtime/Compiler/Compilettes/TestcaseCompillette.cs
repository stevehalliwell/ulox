namespace ULox
{
    public class TestcaseCompillette : ICompilette
    {
        public TokenType Match => TokenType.TESTCASE;
        public string TestCaseName { get; private set; }

        private TestDeclarationCompilette _testDeclarationCompilette;

        public void SetTestDeclarationCompilette(TestDeclarationCompilette testDeclarationCompilette)
        {
            _testDeclarationCompilette = testDeclarationCompilette;
        }

        public void Process(CompilerBase compiler)
        {
            compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect testcase name.");

            var testcaseName = (string)compiler.TokenIterator.PreviousToken.Literal;
            TestCaseName = testcaseName;
            var testDeclName = _testDeclarationCompilette.CurrentTestSetName;
            if (string.IsNullOrEmpty(testDeclName))
                throw new CompilerException($"testcase can only appear within a test set, '{testcaseName}' is not contained in a test declaration.");

            var nameConstantID = compiler.CurrentChunk.AddConstant(Value.New(testcaseName));

            //emit jump // to skip this during imperative
            int testFragmentJump = compiler.EmitJump(OpCode.JUMP);

            _testDeclarationCompilette.AddTestCaseInstruction((ushort)compiler.CurrentChunkInstructinCount);

            compiler.TokenIterator.Consume(TokenType.OPEN_BRACE, "Expect '{' before function body.");

            // The body.
            compiler.EmitOpAndBytes(OpCode.TEST, (byte)TestOpType.CaseStart, nameConstantID, 0x00);

            compiler.BlockStatement();

            compiler.EmitOpAndBytes(OpCode.TEST, (byte)TestOpType.CaseEnd, nameConstantID, 0x00);

            compiler.EmitOpCode(OpCode.NULL);
            compiler.EmitOpAndBytes(OpCode.RETURN, (byte)ReturnMode.One);

            //emit jump to step to next and save it
            compiler.PatchJump(testFragmentJump);
            TestCaseName = null;
        }

        public void TCName(CompilerBase compiler, bool obj)
        {
            var tcname = TestCaseName;
            compiler.AddConstantAndWriteOp(Value.New(tcname));
        }
    }
}
