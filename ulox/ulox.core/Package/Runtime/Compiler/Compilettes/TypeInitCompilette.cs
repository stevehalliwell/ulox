namespace ULox
{
    public class TypeInitCompilette : EmptyTypeBodyCompilette
    {
        public override TokenType Match 
            => TokenType.INIT;

        public override TypeCompiletteStage Stage 
            => TypeCompiletteStage.Init;

        public override void Process(Compiler compiler)
        {
            var initName = ClassCompilette.InitMethodName.String;
            byte constant = compiler.AddCustomStringConstant(initName);
            compiler.Function(initName, FunctionType.Init);
            compiler.EmitOpAndBytes(OpCode.METHOD, constant);
        }
    }
}
