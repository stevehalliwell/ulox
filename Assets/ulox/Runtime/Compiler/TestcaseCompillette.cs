namespace ULox
{
    public enum TestOpType:byte
    {
        Start,
        End,
        InitChain,
    }


    public class TestcaseCompillette : ICompilette
    {
        public TokenType Match => TokenType.TESTCASE;

        private TestDeclarationCompilette _testDeclarationCompilette;

        public TestcaseCompillette(TestDeclarationCompilette testDeclarationCompilette)
        {
            _testDeclarationCompilette = testDeclarationCompilette;
        }

        public void Process(CompilerBase compiler)
        {
            compiler.Consume(TokenType.IDENTIFIER, "Expect testcase name.");

            var compState = compiler.CurrentCompilerState;
            var classCompState = compState.classCompilerStates.Peek();

            var testcaseName = (string)compiler.PreviousToken.Literal;
            var testDeclName = _testDeclarationCompilette.CurrentTestSetName;
            if(string.IsNullOrEmpty(testDeclName))
            {
                throw new VMException($"testcase can only appear within a test set, '{testcaseName}' is not contained in a test declaration.");
            }

            var nameConstantID = compiler.CurrentChunk.AddConstant(Value.New($"{testDeclName}:{testcaseName}"));

            //emit jump // to skip this during imperative
            int testFragmentJump = compiler.EmitJump(OpCode.JUMP);
            //todo could save these to later build a list of tests instruction start locations
            //patch jump previous init fragment if it exists
            if (classCompState.previousTestFragJumpLocation != -1)
            {
                compiler.PatchJump(classCompState.previousTestFragJumpLocation);
            }
            else
            {
                classCompState.testFragStartLocation = compState.chunk.Instructions.Count;
            }

            compiler.Consume(TokenType.OPEN_BRACE, "Expect '{' before function body.");

            // The body.
            compiler.EmitOpAndBytes(OpCode.TEST, (byte)TestOpType.Start, nameConstantID, 0x00);

            compiler.BeginScope();
            compiler.Block();
            compiler.EndScope();
            
            //todo could return rather than jump

            compiler.EmitOpAndBytes(OpCode.TEST, (byte)TestOpType.End, nameConstantID, 0x00);

            classCompState.previousTestFragJumpLocation = compiler.EmitJump(OpCode.JUMP);

            //emit jump to step to next and save it
            compiler.PatchJump(testFragmentJump);
        }
    }
}
