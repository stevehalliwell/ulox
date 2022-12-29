namespace ULox
{
    public sealed class TypePropertyCompilette : ITypeBodyCompilette
    {
        private TypeCompilette _typeCompilette;
        private TokenType _matchToken;
        private bool _requreEndStatement;

        public static TypePropertyCompilette CreateForClass()
        {
            var compilette = new TypePropertyCompilette();
            compilette._matchToken = TokenType.VAR;
            compilette._requreEndStatement = true;
            return compilette;
        }

        public static TypePropertyCompilette CreateForData()
        {
            var compilette = new TypePropertyCompilette();
            compilette._matchToken = TokenType.NONE;
            compilette._requreEndStatement = false;
            return compilette;
        }

        public TokenType Match
            => _matchToken;

        public TypeCompiletteStage Stage
            => TypeCompiletteStage.Var;

        public void Start(TypeCompilette typeCompilette)
        {
            _typeCompilette = typeCompilette;
        }

        public void Process(Compiler compiler)
        {
            do
            {
                compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect var name.");
                byte nameConstant = compiler.AddStringConstant();
                
                compiler.NamedVariable(_typeCompilette.CurrentTypeName, false);
                compiler.EmitPacket(new ByteCodePacket(OpCode.FIELD, nameConstant,0,0));

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

                compiler.EmitOpAndBytes(OpCode.GET_LOCAL, 0);//get class or inst this on the stack

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
                compiler.EmitPacket(new ByteCodePacket(OpCode.SET_PROPERTY, nameConstant,0,0));
                compiler.EmitPop();
                //emit jump // to move to next prop init fragment, defaults to jump nowhere return
                _typeCompilette.PreviousInitFragLabelId = compiler.GotoUniqueChunkLabel("InitFragmentJump");

                //patch jump from skip imperative
                compiler.EmitLabel(initFragmentJump);
            } while (compiler.TokenIterator.Match(TokenType.COMMA));

            if(_requreEndStatement)
                compiler.ConsumeEndStatement("property declaration");
            else
                compiler.TokenIterator.Match(TokenType.END_STATEMENT);
        }
    }
}
