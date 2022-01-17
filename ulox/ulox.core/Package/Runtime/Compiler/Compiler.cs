namespace ULox
{
    public class Compiler : CompilerBase
    {
        private ClassCompilette _classCompiler;
        private TestcaseCompillette _testcaseCompilette;
        private TestDeclarationCompilette _testdec;
        private BuildCompilette _buildCompilette;
        private DependencyInjectionCompilette _diCompiletteParts;

        public Compiler()
        {
            this.SetupSimpleCompiler();
            _testcaseCompilette = new TestcaseCompillette();
            _testdec = new TestDeclarationCompilette();
            _testcaseCompilette.SetTestDeclarationCompilette(_testdec);
            _buildCompilette = new BuildCompilette();
            _classCompiler = new ClassCompilette();
            _diCompiletteParts = new DependencyInjectionCompilette();
            this.AddDeclarationCompilette(
                _testdec,
                _classCompiler,
                _testcaseCompilette,
                _buildCompilette
                                         );

            this.AddStatementCompilette(
                (TokenType.REGISTER, _diCompiletteParts.RegisterStatement),
                (TokenType.FREEZE, FreezeStatement)
                                       );

            this.SetPrattRules(
                (TokenType.THIS, new ActionParseRule(_classCompiler.This, null, Precedence.None)),
                (TokenType.SUPER, new ActionParseRule(_classCompiler.Super, null, Precedence.None)),
                (TokenType.CONTEXT_NAME_CLASS, new ActionParseRule(_classCompiler.CName, null, Precedence.None)),
                (TokenType.CONTEXT_NAME_TESTCASE, new ActionParseRule(_testcaseCompilette.TCName, null, Precedence.None)),
                (TokenType.CONTEXT_NAME_TESTSET, new ActionParseRule(_testdec.TSName, null, Precedence.None)),
                (TokenType.INJECT, new ActionParseRule(_diCompiletteParts.Inject, null, Precedence.Term))
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

        private static void FreezeStatement(CompilerBase compiler)
        {
            compiler.Expression();
            compiler.EmitOpCode(OpCode.FREEZE);
            compiler.ConsumeEndStatement();
        }
    }
}
