namespace ULox
{
    public class TypeMethodCompilette : ITypeBodyCompilette
    {
        public TokenType MatchingToken 
            => TokenType.NONE;

        public TypeCompiletteStage Stage 
            => TypeCompiletteStage.Method;
        

        public void Process(Compiler compiler)
        {
            compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect method name.");
            byte constant = compiler.AddStringConstant();

            var name = compiler.TokenIterator.PreviousToken.Lexeme;
            compiler.Function(name, FunctionType.Method);
            compiler.EmitPacket(new ByteCodePacket(OpCode.METHOD, constant,0,0));
        }

        public void Start(TypeCompilette typeCompilette)
        {
        }
    }
}
