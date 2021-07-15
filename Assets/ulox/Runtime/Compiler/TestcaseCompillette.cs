namespace ULox
{
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
                testDeclName = classCompState.currentClassName;
            }

            var nameConstantID = compiler.CurrentChunk.AddConstant(Value.New($"{testDeclName}:{testcaseName}"));


            //emit jump // to skip this during imperative
            int testFragmentJump = compiler.EmitJump(OpCode.JUMP);
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
            compiler.EmitOpAndByte(OpCode.TEST_START, nameConstantID);

            compiler.BeginScope();
            compiler.Block();
            compiler.EndScope();

            compiler.EmitOpAndByte(OpCode.TEST_END, nameConstantID);

            classCompState.previousTestFragJumpLocation = compiler.EmitJump(OpCode.JUMP);

            //emit jump to step to next and save it
            compiler.PatchJump(testFragmentJump);
        }
    }
}
