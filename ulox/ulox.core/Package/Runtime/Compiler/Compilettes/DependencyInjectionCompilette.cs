namespace ULox
{
    public sealed class DependencyInjectionCompilette
    {
        public void Inject(Compiler compiler, bool canAssign)
        {
            compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect property name after 'inject'.");
            byte name = compiler.AddStringConstant();
            compiler.EmitOpAndBytes(OpCode.INJECT, name);
        }

        public void RegisterStatement(Compiler compiler)
        {
            compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Must provide name after a register statement.");
            var stringConst = compiler.AddStringConstant();
            compiler.Expression();
            compiler.EmitOpAndBytes(OpCode.REGISTER, stringConst);
            compiler.ConsumeEndStatement();
        }
    }
}
