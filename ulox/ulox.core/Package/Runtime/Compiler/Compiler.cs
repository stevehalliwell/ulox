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
                (TokenType.DOT, new ParseRule(null, this.Dot, Precedence.Call)),
                (TokenType.THIS, new ParseRule(This, null, Precedence.None)),
                (TokenType.SUPER, new ParseRule(Super, null, Precedence.None)),
                (TokenType.CONTEXT_NAME_CLASS, new ParseRule(CName, null, Precedence.None)),
                (TokenType.CONTEXT_NAME_TESTCASE, new ParseRule(TCName, null, Precedence.None)),
                (TokenType.CONTEXT_NAME_TESTSET, new ParseRule(TSName, null, Precedence.None)),
                (TokenType.INJECT, new ParseRule(Inject, null, Precedence.Term))
                              );
        }

        private void RegisterStatement(CompilerBase compiler)
        {
            Consume(TokenType.IDENTIFIER, "Must provide name after a register statement.");
            var stringConst = compiler.AddStringConstant();
            Expression();
            EmitOpAndBytes(OpCode.REGISTER, stringConst);
            Consume(TokenType.END_STATEMENT, "Expect ';' after resgister.");
        }

        private void FreezeStatement(CompilerBase compiler)
        {
            Expression();
            EmitOpCode(OpCode.FREEZE);
            Consume(TokenType.END_STATEMENT, "Expect ';' after freeze.");
        }

        private void TCName(bool obj)
        {
            var tcname = _testcaseCompilette.TestCaseName;
            CurrentChunk.AddConstantAndWriteInstruction(Value.New(tcname), PreviousToken.Line);
        }

        private void TSName(bool obj)
        {
            var tsname = _testdec.CurrentTestSetName;
            CurrentChunk.AddConstantAndWriteInstruction(Value.New(tsname), PreviousToken.Line);
        }

        #region Expressions

        protected void This(bool canAssign)
        {
            if (_classCompiler.CurrentTypeName == null)
                throw new CompilerException("Cannot use this outside of a class declaration.");

            NamedVariable("this", canAssign);
        }

        protected void Super(bool canAssign)
        {
            if (_classCompiler.CurrentTypeName == null)
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
            var cname = _classCompiler.CurrentTypeName;
            CurrentChunk.AddConstantAndWriteInstruction(Value.New(cname), PreviousToken.Line);
        }

        protected void Inject(bool canAssign)
        {
            Consume(TokenType.IDENTIFIER, "Expect property name after 'inject'.");
            byte name = AddStringConstant();
            EmitOpAndBytes(OpCode.INJECT, name);
        }

        #endregion Expressions
    }
}
