namespace ULox
{
    public class TypeInitCompilette : ITypeBodyCompilette
    {
        public TokenType Match 
            => TokenType.INIT;

        public TypeCompiletteStage Stage 
            => TypeCompiletteStage.Init;
        
        public void Process(Compiler compiler)
        {
            var initName = TypeCompilette.InitMethodName.String;
            byte constant = compiler.AddCustomStringConstant(initName);
            compiler.Function(initName, FunctionType.Init);
            compiler.EmitOpAndBytes(OpCode.METHOD, constant);
        }

        public void Start(TypeCompilette typeCompilette)
        {
        }
    }
}
