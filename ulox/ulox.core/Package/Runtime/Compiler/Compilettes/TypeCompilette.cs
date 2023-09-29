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
        private TypeInfoEntry _currentTypeInfo;
        public TypeInfoEntry CurrentTypeInfoEntry => _currentTypeInfo;

        public string CurrentTypeName => _currentTypeInfo?.Name ?? null;
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
        {
            PreviousInitFragLabelId = -1;

            Start();

            DoDeclareType(compiler);

            DoClassBody(compiler);

            OnPostBody?.Invoke(compiler);
            OnPostBody = null;

            DoEndType(compiler);

            _currentTypeInfo = null;
        }

        protected virtual void Start()
        {
        }

        private void DoDeclareType(Compiler compiler)
        {
            Stage = TypeCompiletteStage.Begin;
            compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect type name.");
            _currentTypeInfo = new TypeInfoEntry((string)compiler.TokenIterator.PreviousToken.Literal, UserType);
            compiler.PushCompilerState($"{CurrentTypeName}_typedeclare", FunctionType.TypeDeclare);
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
            DoInitChainEnd(compiler);

            compiler.TokenIterator.Consume(TokenType.CLOSE_BRACE, "Expect '}' after class body.");
            compiler.EmitPacket(new ByteCodePacket(OpCode.FREEZE));

            if (IsReadOnlyAtEnd)
            {
                compiler.NamedVariable(CurrentTypeName, false);
                compiler.EmitPacket(new ByteCodePacket(OpCode.READ_ONLY));
            }

            var chunk = compiler.EndCompile();
            compiler.EmitPacket(new ByteCodePacket(OpCode.CLOSURE, new ByteCodePacket.ClosureDetails(ClosureType.Closure, compiler.CurrentChunk.AddConstant(Value.New(chunk)), (byte)chunk.UpvalueCount)));
            compiler.EmitPacket(new ByteCodePacket(OpCode.CALL, 0, 0, 0));
            compiler.EmitPop();

            compiler.TypeInfo.AddType(_currentTypeInfo);
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
