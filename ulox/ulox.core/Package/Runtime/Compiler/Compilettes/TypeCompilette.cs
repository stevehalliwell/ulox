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
        public Label InitChainLabelId { get; private set; } = Label.Default;
        public Label PreviousInitFragLabelId { get; set; } = Label.Default;
        public bool IsReadOnlyAtEnd { get; set; } = false;

        public void CName(Compiler compiler, bool canAssign)
        {
            var cname = CurrentTypeName;
            compiler.AddConstantStringAndWriteOp(cname);
        }

        public abstract UserType UserType { get; }
        public abstract bool EmitClosureCallAtEnd { get; }

        protected abstract void InnerBodyElement(Compiler compiler);

        public void Process(Compiler compiler)
        {
            PreviousInitFragLabelId = Label.Default;

            Start();

            DoDeclareType(compiler);

            DoClassBody(compiler);

            DoInitChainEnd(compiler);
            DoEndType(compiler);
            
            OnPostBody?.Invoke(compiler);
            OnPostBody = null;

            _currentTypeInfo = null;
            InitChainLabelId = Label.Default;
            PreviousInitFragLabelId = Label.Default;
        }

        protected abstract void Start();

        private void DoDeclareType(Compiler compiler)
        {
            Stage = TypeCompiletteStage.Begin;
            compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect type name.");
            _currentTypeInfo = new TypeInfoEntry((string)compiler.TokenIterator.PreviousToken.Literal, UserType); 
            compiler.TypeInfo.AddType(_currentTypeInfo);

            compiler.PushCompilerState($"{CurrentTypeName}", FunctionType.TypeDeclare);
            byte nameConstant = compiler.AddStringConstant();

            InitChainLabelId = compiler.CreateUniqueChunkLabel($"{Chunk.InternalLabelPrefix}InitChain");
            compiler.EmitPacket(new ByteCodePacket(OpCode.FETCH_GLOBAL, nameConstant));

            compiler.TokenIterator.Consume(TokenType.OPEN_BRACE, "Expect '{' before type body.");
        }

        private void DoEndType(Compiler compiler)
        {
            compiler.TokenIterator.Consume(TokenType.CLOSE_BRACE, "Expect '}' after class body.");
            var chunk = compiler.EndCompile();
            if (EmitClosureCallAtEnd)
            {
                compiler.EmitPacket(new ByteCodePacket(new ByteCodePacket.ClosureDetails(ClosureType.Closure, compiler.CurrentChunk.AddConstant(Value.New(chunk)), (byte)chunk.UpvalueCount)));
                compiler.EmitPacket(new ByteCodePacket(OpCode.CALL, 0, 0, 0));
                compiler.EmitPop();
            }
        }

        private void DoInitChainEnd(Compiler compiler)
        {
            //return stub used by init and test chains
            var classReturnEnd = compiler.GotoUniqueChunkLabel("ClassReturnEnd");

            if (PreviousInitFragLabelId != Label.Default)
            {
                compiler.EmitLabel(PreviousInitFragLabelId);
                _currentTypeInfo.PrependInitChain(compiler.CurrentChunk, InitChainLabelId);
            }

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
