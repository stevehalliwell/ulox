namespace ULox
{
    public class TypeStaticElementCompilette : EmptyTypeBodyCompilette
    {
        public override TokenType Match 
            => TokenType.STATIC;

        public override TypeCompiletteStage Stage 
            => TypeCompiletteStage.Static;

        public override void Process(Compiler compiler)
        {
            if (compiler.TokenIterator.Match(TokenType.VAR))
                StaticProperty(compiler);
            else
                StaticMethod(compiler);
        }

        protected void StaticProperty(Compiler compiler)
        {
            do
            {
                compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect var name.");
                byte nameConstant = compiler.AddStringConstant();

                compiler.EmitOpAndBytes(OpCode.GET_LOCAL, 1);//get class or inst this on the stack

                //if = consume it and then
                //eat 1 expression or a push null
                if (compiler.TokenIterator.Match(TokenType.ASSIGN))
                {
                    compiler.Expression();
                }
                else
                {
                    compiler.EmitOpCode(OpCode.NULL);
                }

                //emit set prop
                compiler.EmitOpAndBytes(OpCode.SET_PROPERTY, nameConstant);
                compiler.EmitOpCode(OpCode.POP);
            } while (compiler.TokenIterator.Match(TokenType.COMMA));

            compiler.ConsumeEndStatement();
        }

        protected void StaticMethod(Compiler compiler)
        {
            compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect method name.");
            byte constant = compiler.AddStringConstant();

            var name = compiler.CurrentChunk.ReadConstant(constant).val.asString;

            compiler.Function(name.String, FunctionType.Function);
            compiler.EmitOpAndBytes(OpCode.METHOD, constant);
        }
    }
}
