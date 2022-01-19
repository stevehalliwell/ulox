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
            compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect method name.");
            byte constant = compiler.AddStringConstant();

            var name = compiler.CurrentChunk.ReadConstant(constant).val.asString.String;
            FunctionType funcType = FunctionType.Method;
            compiler.Function(name, funcType);
            compiler.EmitOpAndBytes(OpCode.METHOD, constant);
        }
    }
}
