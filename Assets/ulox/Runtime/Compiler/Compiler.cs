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
            this.AddDeclarationCompilettes(
                _testdec,
                _classCompiler,
                _testcaseCompilette,
                _buildCompilette);

            this.SetPrattRules(
                (TokenType.DOT, new ParseRule(null, this.Dot, Precedence.Call)),
                (TokenType.THIS, new ParseRule(This, null, Precedence.None)),
                (TokenType.SUPER, new ParseRule(Super, null, Precedence.None)),
                (TokenType.CONTEXT_NAME_CLASS, new ParseRule(CName, null, Precedence.None)),
                (TokenType.CONTEXT_NAME_TEST, new ParseRule(TName, null, Precedence.None)),
                (TokenType.CONTEXT_NAME_TESTCASE, new ParseRule(TSName, null, Precedence.None))
                              );
        }

        private void TSName(bool obj)
        {
            var tsname = _testcaseCompilette.TestCaseName;
            CurrentChunk.AddConstantAndWriteInstruction(Value.New(tsname), PreviousToken.Line);
        }

        private void TName(bool obj)
        {
            var tname = _testdec.CurrentTestSetName;
            CurrentChunk.AddConstantAndWriteInstruction(Value.New(tname), PreviousToken.Line);
        }

        #region Expressions

        protected void This(bool canAssign)
        {
            if (_classCompiler.CurrentClassName == null)
                throw new CompilerException("Cannot use this outside of a class declaration.");

            Variable(false);
        }

        protected void Super(bool canAssign)
        {
            if (_classCompiler.CurrentClassName == null)
                throw new CompilerException("Cannot use super outside a class.");

            Consume(TokenType.DOT, "Expect '.' after a super.");
            Consume(TokenType.IDENTIFIER, "Expect superclass method name.");
            var nameID = AddStringConstant();

            NamedVariable("this", false);
            if (Match(TokenType.OPEN_PAREN))
            {
                byte argCount = ArgumentList();
                NamedVariable("super", false);
                EmitOpAndBytes(OpCode.SUPER_INVOKE, nameID);
                EmitBytes(argCount);
            }
            else
            {
                NamedVariable("super", false);
                EmitOpAndBytes(OpCode.GET_SUPER, nameID);
            }
        }

        public void CName(bool canAssign)
        {
            var cname = _classCompiler.CurrentClassName;
            CurrentChunk.AddConstantAndWriteInstruction(Value.New(cname), PreviousToken.Line);
        }

        #endregion Expressions
    }
}
