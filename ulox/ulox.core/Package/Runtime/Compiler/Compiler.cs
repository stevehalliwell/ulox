namespace ULox
{
    public class Compiler : CompilerBase
    {
        private ClassCompilette _classCompiler;
        private TestcaseCompillette _testcaseCompilette;
        private TestDeclarationCompilette _testdec;
        private BuildCompilette _buildCompilette;

        public Compiler()
        {
            this.SetupSimpleCompiler();
            _testcaseCompilette = new TestcaseCompillette();
            _testdec = new TestDeclarationCompilette();
            _testcaseCompilette.SetTestDeclarationCompilette(_testdec);
            _buildCompilette = new BuildCompilette();
            _classCompiler = new ClassCompilette();
            this.AddDeclarationCompilette(
                _testdec,
                _classCompiler,
                _testcaseCompilette,
                _buildCompilette
                                         );

            this.AddStatementCompilette(
                (TokenType.REGISTER, RegisterStatement),
                (TokenType.FREEZE, FreezeStatement)
                                       );

            this.SetPrattRules(
                (TokenType.THIS, new ActionParseRule(_classCompiler.This, null, Precedence.None)),
                (TokenType.SUPER, new ActionParseRule(_classCompiler.Super, null, Precedence.None)),
                (TokenType.CONTEXT_NAME_CLASS, new ActionParseRule(_classCompiler.CName, null, Precedence.None)),
                (TokenType.CONTEXT_NAME_TESTCASE, new ActionParseRule(_testcaseCompilette.TCName, null, Precedence.None)),
                (TokenType.CONTEXT_NAME_TESTSET, new ActionParseRule(_testdec.TSName, null, Precedence.None)),
                (TokenType.INJECT, new ActionParseRule(Inject, null, Precedence.Term))
                              );
        }

        protected override void AfterCompilerStatePushed()
        {
            base.AfterCompilerStatePushed();

            var functionType = CurrentCompilerState.functionType;

            if (functionType == FunctionType.Method || functionType == FunctionType.Init)
                CurrentCompilerState.AddLocal("this", 0);
        }

        protected override void PreEmptyReturnEmit()
        {
            if (CurrentCompilerState.functionType == FunctionType.Init)
                EmitOpAndBytes(OpCode.GET_LOCAL, 0);
            else
                EmitOpCode(OpCode.NULL);
        }

        private static void RegisterStatement(CompilerBase compiler)
        {
            compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Must provide name after a register statement.");
            var stringConst = compiler.AddStringConstant();
            compiler.Expression();
            compiler.EmitOpAndBytes(OpCode.REGISTER, stringConst);
            compiler.ConsumeEndStatement();
        }

        private static void FreezeStatement(CompilerBase compiler)
        {
            compiler.Expression();
            compiler.EmitOpCode(OpCode.FREEZE);
            compiler.ConsumeEndStatement();
        }

        #region Expressions

        protected static void Inject(CompilerBase compiler, bool canAssign)
        {
            compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect property name after 'inject'.");
            byte name = compiler.AddStringConstant();
            compiler.EmitOpAndBytes(OpCode.INJECT, name);
        }

        #endregion Expressions
    }
}
