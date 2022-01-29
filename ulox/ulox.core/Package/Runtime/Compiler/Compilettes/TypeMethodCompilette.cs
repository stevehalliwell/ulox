namespace ULox
{
    public class TypeMethodCompilette : EmptyTypeBodyCompilette
    {
        public override TokenType Match 
            => TokenType.NONE;

        public override TypeCompiletteStage Stage 
            => TypeCompiletteStage.Method;

        public override void Process(Compiler compiler)
        {
            var isLocal = false;
            if (compiler.TokenIterator.Match(TokenType.LOCAL))
                isLocal = true;

            compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect method name.");
            byte constant = compiler.AddStringConstant();

            var name = compiler.TokenIterator.PreviousToken.Lexeme;
            FunctionType funcType = isLocal 
                ? FunctionType.LocalFunction 
                : FunctionType.Method;
            compiler.Function(name, funcType);
            compiler.EmitOpAndBytes(OpCode.METHOD, constant);
        }
    }
}
