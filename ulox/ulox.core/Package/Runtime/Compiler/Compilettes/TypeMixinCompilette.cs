namespace ULox
{
    public class TypeMixinCompilette : ITypeBodyCompilette
    {
        private TypeCompilette _typeCompilette;

        public TokenType MatchingToken
            => TokenType.MIXIN;
        public TypeCompiletteStage Stage
            => TypeCompiletteStage.Mixin;

        public void Start(TypeCompilette typeCompilette)
        {
            _typeCompilette = typeCompilette;
        }

        public void Process(Compiler compiler)
        {
            do
            {
                compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect identifier after mixin into class.");
                compiler.NamedVariable(compiler.TokenIterator.PreviousToken.Literal as string, false);
                compiler.NamedVariable(_typeCompilette.CurrentTypeName, false);
                compiler.EmitPacket( new ByteCodePacket(OpCode.MIXIN));
            } while (compiler.TokenIterator.Match(TokenType.COMMA));
            
            compiler.ConsumeEndStatement("mixin declaration");
        }
    }
}
