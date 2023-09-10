namespace ULox
{
    public class TypeInitCompilette : ITypeBodyCompilette
    {
        public TokenType MatchingToken 
            => TokenType.INIT;

        public TypeCompiletteStage Stage 
            => TypeCompiletteStage.Init;
        
        public void Process(Compiler compiler)
        {
            var initName = ClassTypeCompilette.InitMethodName.String;
            byte constant = compiler.AddCustomStringConstant(initName);
            compiler.Function(initName, FunctionType.Init);
            compiler.EmitPacket(new ByteCodePacket(OpCode.METHOD, constant,0,0));
        }
    }
}
