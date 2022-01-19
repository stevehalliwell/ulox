namespace ULox
{
    public class ClassCompilette : TypeCompilette
    {
        public ClassCompilette()
        {
            AddInnerDeclarationCompilette(new TypeStaticElementCompilette());
            AddInnerDeclarationCompilette(new TypeInitCompilette());
            AddInnerDeclarationCompilette(new TypeMethodCompilette());
            AddInnerDeclarationCompilette(new TypeMixinCompilette(this));
            AddInnerDeclarationCompilette(new TypePropertyCompilette(this));

            GenerateCompiletteByStageArray();
        }

        public override TokenType Match 
            => TokenType.CLASS;

        public override void Process(Compiler compiler) 
            => ClassDeclaration(compiler);

        private void ClassDeclaration(Compiler compiler)
        {
            foreach (var bodyCompilette in _stageOrderedBodyCompilettes)
                bodyCompilette.Start();

            DoBeginDeclareType(compiler);
            DoDeclareLineInher(compiler);
            DoEndDeclareType(compiler);

            foreach (var bodyCompilette in _stageOrderedBodyCompilettes)
                bodyCompilette.PreBody(compiler);

            DoClassBody(compiler);

            foreach (var bodyCompilette in _stageOrderedBodyCompilettes)
                bodyCompilette.PostBody(compiler);

            DoEndType(compiler);

            foreach (var bodyCompilette in _stageOrderedBodyCompilettes)
                bodyCompilette.End();

            CurrentTypeName = null;
        }

        public void CName(Compiler compiler, bool canAssign)
        {
            var cname = CurrentTypeName;
            compiler.AddConstantAndWriteOp(Value.New(cname));
        }

        public void This(Compiler compiler, bool canAssign)
        {
            if (CurrentTypeName == null)
                throw new CompilerException("Cannot use this outside of a class declaration.");

            compiler.NamedVariable("this", canAssign);
        }

        public void Super(Compiler compiler, bool canAssign)
        {
            if (CurrentTypeName == null)
                throw new CompilerException("Cannot use super outside a class.");

            compiler.TokenIterator.Consume(TokenType.DOT, "Expect '.' after a super.");
            compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect superclass method name.");
            var nameID = compiler.AddStringConstant();

            compiler.NamedVariable("this", false);
            if (compiler.TokenIterator.Match(TokenType.OPEN_PAREN))
            {
                byte argCount = compiler.ArgumentList();
                compiler.NamedVariable("super", false);
                compiler.EmitOpAndBytes(OpCode.SUPER_INVOKE, nameID);
                compiler.EmitBytes(argCount);
            }
            else
            {
                compiler.NamedVariable("super", false);
                compiler.EmitOpAndBytes(OpCode.GET_SUPER, nameID);
            }
        }
    }
}
