namespace ULox
{
    public sealed class TypeInstancePropertyCompilette : ITypeBodyCompilette
    {
        private readonly TypeCompilette _typeCompilette;

        public TokenType MatchingToken
            => TokenType.VAR;

        public TypeCompiletteStage Stage
            => TypeCompiletteStage.Var;

        public TypeInstancePropertyCompilette(TypeCompilette typeCompilette)
        {
            _typeCompilette = typeCompilette;
        }

        public void Process(Compiler compiler)
        {
            do
            {
                compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect var name");
                byte nameConstant = compiler.AddStringConstant();

                compiler.NamedVariable(_typeCompilette.CurrentTypeName, false);
                compiler.EmitPacket(new ByteCodePacket(OpCode.FIELD, nameConstant));

                //emit jump // to skip this during imperative
                var initFragmentJump = compiler.GotoUniqueChunkLabel("SkipInitDuringImperative");
                //patch jump previous init fragment if it exists
                if (_typeCompilette.PreviousInitFragLabelId != -1)
                {
                    compiler.EmitLabel((byte)_typeCompilette.PreviousInitFragLabelId);
                }
                else
                {
                    compiler.EmitLabel((byte)_typeCompilette.InitChainLabelId);
                }

                compiler.EmitPacket(new ByteCodePacket(OpCode.GET_LOCAL, (byte)0));//get class or inst this on the stack

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
                compiler.EmitPacket(new ByteCodePacket(OpCode.SET_PROPERTY, nameConstant));
                compiler.EmitPop();
                //emit jump // to move to next prop init fragment, defaults to jump nowhere return
                _typeCompilette.PreviousInitFragLabelId = compiler.GotoUniqueChunkLabel("InitFragmentJump");

                //patch jump from skip imperative
                compiler.EmitLabel(initFragmentJump);

                //if trailing comma, eat it
                compiler.TokenIterator.Match(TokenType.COMMA);
            } while (compiler.TokenIterator.Check(TokenType.IDENTIFIER));

            compiler.TokenIterator.Match(TokenType.END_STATEMENT);
        }
    }
}
