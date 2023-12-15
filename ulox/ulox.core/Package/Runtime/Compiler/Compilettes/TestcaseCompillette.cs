﻿namespace ULox
{
    public class TestcaseCompillette : ICompilette
    {
        public TokenType MatchingToken => TokenType.TESTCASE;
        public string TestCaseName { get; private set; }

        private const string AnonTestPrefix = "Anon_Test_";
        private const string TestDataSourceVarName = "testDataSource";
        private const string TestDataRowVarName = "testDataRow";
        private const string TestDataIndexVarName = "testDataIndex";
        private readonly TestSetDeclarationCompilette _testDeclarationCompilette;

        public TestcaseCompillette(TestSetDeclarationCompilette testDeclarationCompilette)
        {
            _testDeclarationCompilette = testDeclarationCompilette;
        }

        public void Process(Compiler compiler)
        {
            var dataExpExecuteLocation = -1;
            var dataExpJumpBackToStart = -1;
            var testDataIndexLocalId = byte.MaxValue;
            var testDataSourceLocalId = byte.MaxValue;
            var exitDataLoopJumpLoc = -1;
            var preRowCountCheck = -1;
                
            compiler.BeginScope();
            
            if (compiler.TokenIterator.Match(TokenType.OPEN_PAREN))
            {
                //jump
                var dataExpJumpID = compiler.GotoUniqueChunkLabel($"DataExpJump_{_testDeclarationCompilette.CurrentTestSetName}");
                //note location
                dataExpExecuteLocation = compiler.LabelUniqueChunkLabel("DataExpExecuteLocation");

                //expression
                compiler.DeclareAndDefineCustomVariable(TestDataSourceVarName);
                compiler.EmitNULL();
                compiler.DeclareAndDefineCustomVariable(TestDataRowVarName);
                compiler.EmitNULL();
                compiler.DeclareAndDefineCustomVariable(TestDataIndexVarName);
                compiler.EmitPacket(new ByteCodePacket(new ByteCodePacket.PushValueDetails(0)));
                compiler.Expression();
                var res = compiler.ResolveLocal(TestDataSourceVarName);
                testDataSourceLocalId = (byte)res;
                compiler.EmitPacket(new ByteCodePacket(OpCode.SET_LOCAL, testDataSourceLocalId));
                compiler.EmitPop();

                //jump for moving back to start
                dataExpJumpBackToStart = compiler.GotoUniqueChunkLabel($"DataExpJumpBackToStart_{_testDeclarationCompilette.CurrentTestSetName}");

                //patch data jump
                compiler.EmitLabel(dataExpJumpID);

                //temp
                compiler.TokenIterator.Consume(TokenType.CLOSE_PAREN, "");
            }

            var testcaseName = compiler.IdentifierOrChunkUnique(AnonTestPrefix);
            TestCaseName = testcaseName;
            var testDeclName = _testDeclarationCompilette.CurrentTestSetName;
            if (string.IsNullOrEmpty(testDeclName))
                compiler.ThrowCompilerException($"Unexpected test, it can only appear within a testset, '{testcaseName}' is not contained in a testset declaration.");

            var nameConstantID = compiler.CurrentChunk.AddConstant(Value.New(testcaseName));

            //emit jump // to skip this during imperative
            var testFragmentJump = compiler.GotoUniqueChunkLabel("testFragmentJump");

            _testDeclarationCompilette.AddTestCaseLabel(compiler.LabelUniqueChunkLabel($"TestCase_{testcaseName}"));

            compiler.BeginScope();
            var numArgs = compiler.VariableNameListDeclareOptional(null);
            if (numArgs != 0 && dataExpExecuteLocation == -1)
                compiler.ThrowCompilerException($"Test '{testcaseName}' has arguments but no data expression");
            if (numArgs == 0 && dataExpExecuteLocation != -1)
                compiler.ThrowCompilerException($"Test '{testcaseName}' has data expression but no arguments");

            compiler.TokenIterator.Consume(TokenType.OPEN_BRACE, "Expect '{' before test body.");

            //jump back
            if (dataExpExecuteLocation != -1)
            {
                compiler.EmitGoto((byte)dataExpExecuteLocation);
                compiler.EmitLabel((byte)dataExpJumpBackToStart);

                //need to deal with the args
                //get testset data row
                var testDataIndexLocalIdRes = compiler.ResolveLocal(TestDataIndexVarName);
                testDataIndexLocalId = testDataIndexLocalIdRes;

                preRowCountCheck = compiler.LabelUniqueChunkLabel("preRowCountCheck");

                IsIndexLessThanArrayCount(compiler, OpCode.GET_LOCAL, testDataSourceLocalId, testDataIndexLocalId);
                exitDataLoopJumpLoc = compiler.GotoIfUniqueChunkLabel("exitDataLoopJumpLoc");
                compiler.EmitPop(); // Condition.

                //get row from array index
                compiler.EmitPacket(new ByteCodePacket(OpCode.GET_LOCAL, testDataSourceLocalId));
                compiler.EmitPacket(new ByteCodePacket(OpCode.GET_LOCAL, testDataIndexLocalId));
                compiler.EmitPacket(new ByteCodePacket(OpCode.GET_INDEX));
                var testDataRowLocalId = compiler.ResolveLocal(TestDataRowVarName);
                compiler.EmitPacket(new ByteCodePacket(OpCode.SET_LOCAL, testDataRowLocalId));
                compiler.EmitPop();
                //appy row as inputs to the test
                compiler.EmitPacket(new ByteCodePacket(OpCode.GET_LOCAL, testDataRowLocalId));
                compiler.EmitPacket(new ByteCodePacket(OpCode.EXPAND_COPY_TO_STACK));
            }

            // The body.
            TestSetDeclarationCompilette.EmitTestPacket(compiler, TestOpType.CaseStart, nameConstantID, numArgs);
            compiler.BlockStatement();
            TestSetDeclarationCompilette.EmitTestPacket(compiler, TestOpType.CaseEnd, nameConstantID, 0);
            compiler.EndScope();

            if (dataExpExecuteLocation != -1)
            {
                IncrementLocalByOne(compiler, testDataIndexLocalId);
                compiler.EmitGoto((byte)preRowCountCheck);
                compiler.EmitLabel((byte)exitDataLoopJumpLoc);
            }

            compiler.EmitReturn();

            compiler.EndScope();
            
            //emit jump to step to next and save it
            compiler.EmitLabel(testFragmentJump);
            TestCaseName = null;
        }

        public void TestName(Compiler compiler, bool obj)
        {
            var tcname = TestCaseName;
            compiler.AddConstantAndWriteOp(Value.New(tcname));
        }
        
        public static void IsIndexLessThanArrayCount(Compiler compiler, OpCode arrayGetOp, byte arrayArgId, byte indexArgID)
        {
            compiler.EmitPacket(new ByteCodePacket(OpCode.GET_LOCAL, indexArgID));
            compiler.EmitPacket(new ByteCodePacket(arrayGetOp, arrayArgId));
            compiler.EmitPacket(new ByteCodePacket(OpCode.COUNT_OF));
            compiler.EmitPacket(new ByteCodePacket(OpCode.LESS));
        }

        public static void IncrementLocalByOne(Compiler compiler, byte indexArgID)
        {
            compiler.EmitPacket(new ByteCodePacket(OpCode.GET_LOCAL, indexArgID));
            compiler.EmitPacket(new ByteCodePacket(new ByteCodePacket.PushValueDetails(1)));
            compiler.EmitPacket(new ByteCodePacket(OpCode.ADD));
            compiler.EmitPacket(new ByteCodePacket(OpCode.SET_LOCAL, indexArgID));
            compiler.EmitPop();
        }
    }
}
