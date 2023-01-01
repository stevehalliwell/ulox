namespace ULox
{
    public class TypeStaticElementCompilette : ITypeBodyCompilette
    {
        public TokenType Match 
            => TokenType.STATIC;
        
        public TypeCompiletteStage Stage 
            => TypeCompiletteStage.Static;

        public void Process(Compiler compiler)
        {
            if (compiler.TokenIterator.Match(TokenType.VAR))
                StaticProperty(compiler);
            else
                StaticMethod(compiler);
        }

        protected static void StaticProperty(Compiler compiler)
        {
            do
            {
                compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect var name.");
                byte nameConstant = compiler.AddStringConstant();

                compiler.EmitPacketByte(OpCode.GET_LOCAL, 1);//get class or inst this on the stack

                //if = consume it and then
                //eat 1 expression or a push null
                if (compiler.TokenIterator.Match(TokenType.ASSIGN))
                {
                    compiler.Expression();
                }
                else
                {
                    compiler.EmitNULL();
                }

                //emit set prop
                compiler.EmitPacket(new ByteCodePacket(OpCode.SET_PROPERTY, nameConstant, 0, 0));
                compiler.EmitPop();
            } while (compiler.TokenIterator.Match(TokenType.COMMA));

            compiler.ConsumeEndStatement();
        }

        protected static void StaticMethod(Compiler compiler)
        {
            compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect method name.");
            byte constant = compiler.AddStringConstant();

            var name = compiler.CurrentChunk.ReadConstant(constant).val.asString;

            compiler.Function(name.String, FunctionType.Function);
            compiler.EmitPacket(new ByteCodePacket(OpCode.METHOD, constant, 0, 0));
        }

        public void Start(TypeCompilette typeCompilette)
        {
        }
    }
}
