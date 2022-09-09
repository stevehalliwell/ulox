namespace ULox
{
    public class ClassCompilette : TypeCompilette
    {
        public ClassCompilette()
        {
            AddInnerDeclarationCompilette(new TypeStaticElementCompilette());
            AddInnerDeclarationCompilette(new TypeInitCompilette());
            AddInnerDeclarationCompilette(new TypeMethodCompilette());
            AddInnerDeclarationCompilette(new TypeSignsCompilette());
            AddInnerDeclarationCompilette(new TypeMixinCompilette());
            AddInnerDeclarationCompilette(new TypePropertyCompilette());

            GenerateCompiletteByStageArray();
        }

        public override TokenType Match 
            => TokenType.CLASS;

        public override void Process(Compiler compiler) 
            => ClassDeclaration(compiler);

        private void ClassDeclaration(Compiler compiler)
        {
            foreach (var bodyCompilette in BodyCompilettesProcessingOrdered)
                bodyCompilette.Start(this);

            DoBeginDeclareType(compiler);
            DoEndDeclareType(compiler);

            foreach (var bodyCompilette in BodyCompilettesProcessingOrdered)
                bodyCompilette.PreBody(compiler);

            DoClassBody(compiler);

            foreach (var bodyCompilette in BodyCompilettesPostBodyOrdered)
                bodyCompilette.PostBody(compiler);

            DoEndType(compiler);

            foreach (var bodyCompilette in BodyCompilettesProcessingOrdered)
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
                compiler.ThrowCompilerException("Cannot use the 'this' keyword outside of a class");

            compiler.NamedVariable("this", canAssign);
        }
    }
}
