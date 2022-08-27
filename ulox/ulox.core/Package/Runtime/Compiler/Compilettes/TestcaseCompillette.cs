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

        public void Process(Compiler compiler)
        {
            var hasData = false;
            var dataExpJumpToPatch = -1;
            var dataExpExecuteLocation = -1;
            var dataExpJumpBackToStart = -1;
            var testDataIndexLocalId = byte.MaxValue;
            var testDataSourceLocalId = byte.MaxValue;
            var exitDataLoopJumpLoc = -1;
            var preRowCountCheck = -1;
            if (compiler.TokenIterator.Match(TokenType.OPEN_PAREN))
            {
                compiler.BeginScope();
                //jump
                dataExpJumpToPatch = compiler.EmitJump();
                //note location
                dataExpExecuteLocation = compiler.CurrentChunkInstructinCount;

                //expression
                compiler.DeclareAndDefineCustomVariable("testDataSource");
                compiler.EmitOpCode(OpCode.NULL);
                compiler.DeclareAndDefineCustomVariable("testDataRow");
                compiler.EmitOpCode(OpCode.NULL);
                compiler.DeclareAndDefineCustomVariable("testDataIndex");
                compiler.EmitOpAndBytes(OpCode.PUSH_BYTE, 0);
                compiler.Expression();
                var (_,_, res) = compiler.ResolveNameLookupOpCode("testDataSource");
                testDataSourceLocalId = res;
                compiler.EmitOpAndBytes(OpCode.SET_LOCAL,  testDataSourceLocalId);
                compiler.EmitOpCode(OpCode.POP);

                //jump for moving back to start
                dataExpJumpBackToStart = compiler.EmitJump();

                //patch jump
                compiler.PatchJump(dataExpJumpToPatch);

                //temp
                compiler.TokenIterator.Consume(TokenType.CLOSE_PAREN, "");
                hasData = true;
            }

            compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect testcase name.");

            var testcaseName = (string)compiler.TokenIterator.PreviousToken.Literal;
            TestCaseName = testcaseName;
            var testDeclName = _testDeclarationCompilette.CurrentTestSetName;
            if (string.IsNullOrEmpty(testDeclName))
                throw new CompilerException($"testcase can only appear within a test set, '{testcaseName}' is not contained in a test declaration.");

            var nameConstantID = compiler.CurrentChunk.AddConstant(Value.New(testcaseName));

            //emit jump // to skip this during imperative
            int testFragmentJump = compiler.EmitJump();

            _testDeclarationCompilette.AddTestCaseInstruction((ushort)compiler.CurrentChunkInstructinCount);
            
            compiler.BeginScope();
            var numArgs = compiler.VariableNameListDeclareOptional(null);
            if (numArgs != 0 && dataExpExecuteLocation == -1)
                throw new CompilerException($"Testcase '{testcaseName}' has arguments but no data expression.");
            if (numArgs == 0 && dataExpExecuteLocation != -1)
                throw new CompilerException($"Testcase '{testcaseName}' has data expression but no arguments.");

            compiler.TokenIterator.Consume(TokenType.OPEN_BRACE, "Expect '{' before testcase body.");
            
            //jump back
            if (dataExpExecuteLocation != -1)
            {
                compiler.EmitLoop(dataExpExecuteLocation);
                compiler.PatchJump(dataExpJumpBackToStart);

                //need to deal with the args
                //get test data row
                var (_, _, testDataIndexLocalIdRes) = compiler.ResolveNameLookupOpCode("testDataIndex");
                testDataIndexLocalId = testDataIndexLocalIdRes;

                preRowCountCheck = compiler.CurrentChunkInstructinCount;
                
                LoopStatementCompilette.IsIndexLessThanArrayCount(compiler, OpCode.GET_LOCAL, testDataSourceLocalId, testDataIndexLocalId);
                exitDataLoopJumpLoc = compiler.EmitJumpIf();
                compiler.EmitOpCode(OpCode.POP); // Condition.

                //get row from array index
                compiler.EmitOpAndBytes(OpCode.GET_LOCAL, testDataSourceLocalId);
                compiler.EmitOpAndBytes(OpCode.GET_LOCAL, testDataIndexLocalId);
                compiler.EmitOpCode(OpCode.GET_INDEX);
                var (_, _, testDataRowLocalId) = compiler.ResolveNameLookupOpCode("testDataRow");
                compiler.EmitOpAndBytes(OpCode.SET_LOCAL, testDataRowLocalId);
                compiler.EmitOpCode(OpCode.POP);
                //appy row as inputs to the test 
                compiler.EmitOpAndBytes(OpCode.GET_LOCAL, testDataRowLocalId);
                compiler.EmitOpCode(OpCode.EXPAND_COPY_TO_STACK);

            }

            // The body.
            compiler.EmitOpAndBytes(OpCode.TEST, (byte)TestOpType.CaseStart, nameConstantID, numArgs);
            compiler.BlockStatement();
            compiler.EmitOpAndBytes(OpCode.TEST, (byte)TestOpType.CaseEnd, nameConstantID, 0x00);
            compiler.EndScope();

            if (dataExpExecuteLocation != -1)
            {
                LoopStatementCompilette.IncrementLocalByOne(compiler, testDataIndexLocalId);
                compiler.EmitLoop(preRowCountCheck);
                compiler.PatchJump(exitDataLoopJumpLoc);
            }

            compiler.EmitOpCode(OpCode.NULL);
            compiler.EmitOpAndBytes(OpCode.RETURN, (byte)ReturnMode.One);


            if (dataExpExecuteLocation != -1)
            {
                compiler.EndScope();
            }
            
            //emit jump to step to next and save it
            compiler.PatchJump(testFragmentJump);
            TestCaseName = null;
        }

        public void TCName(Compiler compiler, bool obj)
        {
            var tcname = TestCaseName;
            compiler.AddConstantAndWriteOp(Value.New(tcname));
        }
    }
}