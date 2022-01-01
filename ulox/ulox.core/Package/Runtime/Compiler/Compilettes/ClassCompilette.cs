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

        public override TokenType Match => TokenType.CLASS;

        public override void Process(CompilerBase compiler) => ClassDeclaration(compiler);

        private void ClassDeclaration(CompilerBase compiler)
        {
            foreach (var bodyCompilette in _stageOrderedBodyCompilettes)
                bodyCompilette.Start();

            DoBeginDeclareType(compiler, out var compState);
            DoDeclareLineInher(compiler, compState);
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
    }
}
