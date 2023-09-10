namespace ULox
{
    public enum UserType : byte
    {
        Native,
        Class,
        Enum,
    }

    public abstract class TypeCompilette : ICompilette
    {
        public abstract TokenType MatchingToken { get; }

        protected TypeCompiletteStage Stage = TypeCompiletteStage.Invalid;
        public string CurrentTypeName { get; private set; }
        public event System.Action<Compiler> OnPostBody;
        public byte InitChainLabelId { get; private set; }
        public int PreviousInitFragLabelId { get; set; } = -1;
        public bool IsReadOnlyAtEnd { get; set; } = false;

        public void CName(Compiler compiler, bool canAssign)
        {
            var cname = CurrentTypeName;
            compiler.AddConstantAndWriteOp(Value.New(cname));
        }

        public abstract UserType UserType { get; }

        protected abstract void InnerBodyElement(Compiler compiler);

        public void Process(Compiler compiler)
            => InnerProcess(compiler);

        private void InnerProcess(Compiler compiler)
        {
            PreviousInitFragLabelId = -1;

            Start();

            DoDeclareType(compiler);

            DoClassBody(compiler);

            DoInitChainEnd(compiler);

            OnPostBody?.Invoke(compiler);
            OnPostBody = null;

            DoEndType(compiler);

            CurrentTypeName = null;
        }

        protected virtual void Start()
        {
        }

        private void DoInitChainEnd(Compiler compiler)
        {
            //return stub used by init and test chains
            var classReturnEnd = compiler.GotoUniqueChunkLabel("ClassReturnEnd");

            if (PreviousInitFragLabelId != -1)
                compiler.EmitLabel((byte)PreviousInitFragLabelId);
            else
                compiler.CurrentCompilerState.chunk.AddLabel(InitChainLabelId, 0);

            compiler.EmitPacket(new ByteCodePacket(OpCode.RETURN));

            compiler.EmitLabel(classReturnEnd);
        }

        private void DoDeclareType(Compiler compiler)
        {
            Stage = TypeCompiletteStage.Begin;
            compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect type name.");
            CurrentTypeName = (string)compiler.TokenIterator.PreviousToken.Literal;
            byte nameConstant = compiler.AddStringConstant();
            compiler.DeclareVariable();

            InitChainLabelId = compiler.UniqueChunkLabelStringConstant("InitChain");
            compiler.EmitPacket(new ByteCodePacket(OpCode.TYPE, new ByteCodePacket.TypeDetails(nameConstant, UserType, InitChainLabelId)));

            compiler.DefineVariable(nameConstant);
            compiler.NamedVariable(CurrentTypeName, false);
            compiler.TokenIterator.Consume(TokenType.OPEN_BRACE, "Expect '{' before type body.");
        }

        private void DoEndType(Compiler compiler)
        {
            compiler.TokenIterator.Consume(TokenType.CLOSE_BRACE, "Expect '}' after class body.");
            compiler.EmitPacket(new ByteCodePacket(OpCode.FREEZE));

            if (IsReadOnlyAtEnd)
            {
                compiler.NamedVariable(CurrentTypeName, false);
                compiler.EmitPacket(new ByteCodePacket(OpCode.READ_ONLY));
            }
        }

        private void DoClassBody(Compiler compiler)
        {
            while (!compiler.TokenIterator.Check(TokenType.CLOSE_BRACE) && !compiler.TokenIterator.Check(TokenType.EOF))
            {
                InnerBodyElement(compiler);
            }

            Stage = TypeCompiletteStage.Complete;
        }
    }
}
