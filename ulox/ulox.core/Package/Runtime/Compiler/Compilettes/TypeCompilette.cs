﻿using System.Collections.Generic;
using System.Linq;

namespace ULox
{
    public enum UserType : byte
    {
        Native,
        Data,
        System,
        Class,
        Enum,
    }

    public sealed class TypeCompilette : ICompilette
    {
        private TypeCompilette() { }

        public static TypeCompilette CreateClassCompilette()
        {
            var compilette = new TypeCompilette();
            compilette.AddInnerDeclarationCompilette(TypeStaticElementCompilette.CreateForClass());
            compilette.AddInnerDeclarationCompilette(new TypeInitCompilette());
            compilette.AddInnerDeclarationCompilette(new TypeMethodCompilette());
            compilette.AddInnerDeclarationCompilette(new TypeSignsCompilette());
            compilette.AddInnerDeclarationCompilette(new TypeMixinCompilette());
            compilette.AddInnerDeclarationCompilette(TypePropertyCompilette.CreateForClass());
            compilette.GenerateCompiletteByStageArray();
            compilette.UserType = UserType.Class;
            compilette.MatchingToken = TokenType.CLASS;
            return compilette;
        }

        public static TypeCompilette CreateDataCompilette()
        {
            var compilette = new TypeCompilette();
            compilette.AddInnerDeclarationCompilette(new TypeSignsCompilette());
            compilette.AddInnerDeclarationCompilette(new TypeMixinCompilette());
            compilette.AddInnerDeclarationCompilette(TypePropertyCompilette.CreateForData());
            compilette.GenerateCompiletteByStageArray();
            compilette.UserType = UserType.Data;
            compilette.MatchingToken = TokenType.DATA;
            return compilette;
        }

        public static TypeCompilette CreateSystemCompilette()
        {
            var compilette = new TypeCompilette();
            compilette.AddInnerDeclarationCompilette(new TypeSignsCompilette());
            compilette.AddInnerDeclarationCompilette(new TypeMixinCompilette());
            compilette.AddInnerDeclarationCompilette(TypeStaticElementCompilette.CreateForSystem());
            compilette.GenerateCompiletteByStageArray();
            compilette.UserType = UserType.System;
            compilette.MatchingToken = TokenType.SYSTEM;
            return compilette;
        }

        public static TypeCompilette CreateEnumCompilette()
        {
            var compilette = new TypeCompilette();
            compilette.AddInnerDeclarationCompilette(new TypeEnumValueCompilette());
            compilette.GenerateCompiletteByStageArray();
            compilette.UserType = UserType.Enum;
            compilette.MatchingToken = TokenType.ENUM;
            compilette.IsReadOnlyAtEnd = true;
            return compilette;
        }

        private static readonly TypeCompiletteStage[] BodyCompileStageOrder = new[]
        {
            TypeCompiletteStage.Invalid,
            TypeCompiletteStage.Begin,
            TypeCompiletteStage.Static,
            TypeCompiletteStage.Mixin,
            TypeCompiletteStage.Signs,
            TypeCompiletteStage.Var,
            TypeCompiletteStage.Init,
            TypeCompiletteStage.Method,
            TypeCompiletteStage.Complete
        };

        public TokenType MatchingToken { get; private set; }

        public static readonly HashedString InitMethodName = new HashedString("init");

        private readonly Dictionary<TokenType, ITypeBodyCompilette> _innerDeclarationCompilettes = new Dictionary<TokenType, ITypeBodyCompilette>();
        private ITypeBodyCompilette _bodyCompiletteFallback;

        private TypeCompiletteStage _stage = TypeCompiletteStage.Invalid;
        public string CurrentTypeName { get; private set; }
        public event System.Action<Compiler> OnPostBody;
        public byte InitChainLabelId { get; private set; }
        public int PreviousInitFragLabelId { get; set; } = -1;
        public bool IsFrozenAtEnd { get; set; } = true;
        public bool IsReadOnlyAtEnd { get; set; } = false;
        private ITypeBodyCompilette[] BodyCompilettesProcessingOrdered;

        public void AddInnerDeclarationCompilette(ITypeBodyCompilette compilette)
        {
            _innerDeclarationCompilettes[compilette.MatchingToken] = compilette;
            if (compilette.MatchingToken == TokenType.NONE)
                _bodyCompiletteFallback = compilette;
        }

        public void CName(Compiler compiler, bool canAssign)
        {
            var cname = CurrentTypeName;
            compiler.AddConstantAndWriteOp(Value.New(cname));
        }

        public UserType UserType { get; private set; }

        public void Process(Compiler compiler)
            => StageBasedDeclaration(compiler);

        private void StageBasedDeclaration(Compiler compiler)
        {
            PreviousInitFragLabelId = -1;

            foreach (var bodyCompilette in BodyCompilettesProcessingOrdered)
                bodyCompilette.Start(this);

            DoDeclareType(compiler);

            DoClassBody(compiler);

            DoInitChainEnd(compiler);

            OnPostBody?.Invoke(compiler);
            OnPostBody = null;

            DoEndType(compiler);

            CurrentTypeName = null;
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

        private void GenerateCompiletteByStageArray()
        {
            BodyCompilettesProcessingOrdered = _innerDeclarationCompilettes.Values
                .OrderBy(x => System.Array.IndexOf(BodyCompileStageOrder, x.Stage))
                .ToArray();
        }

        private void DoDeclareType(Compiler compiler)
        {
            _stage = TypeCompiletteStage.Begin;
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
            if (IsFrozenAtEnd)
                compiler.EmitPacket(new ByteCodePacket(OpCode.FREEZE));
            else
                compiler.EmitPop();
            
            if(IsReadOnlyAtEnd)
            {
                compiler.NamedVariable(CurrentTypeName, false);
                compiler.EmitPacket(new ByteCodePacket(OpCode.READ_ONLY));
            }
        }

        private void DoClassBody(Compiler compiler)
        {
            while (!compiler.TokenIterator.Check(TokenType.CLOSE_BRACE) && !compiler.TokenIterator.Check(TokenType.EOF))
            {
                var compilette = _bodyCompiletteFallback;
                if (_innerDeclarationCompilettes.TryGetValue(compiler.TokenIterator.CurrentToken.TokenType, out var matchingCompilette))
                {
                    compiler.TokenIterator.Advance();
                    compilette = matchingCompilette;
                }

                ValidStage(compiler, compilette.Stage);
                compilette.Process(compiler);
            }

            _stage = TypeCompiletteStage.Complete;
        }

        private void ValidStage(Compiler compiler, TypeCompiletteStage stage)
        {
            if (_stage > stage)
                compiler.ThrowCompilerException($"Stage out of order. Type '{CurrentTypeName}' is at stage '{_stage}' has encountered a late '{stage}' stage element");

            _stage = stage;
        }

        public void This(Compiler compiler, bool canAssign)
        {
            if (CurrentTypeName == null)
                compiler.ThrowCompilerException("Cannot use the 'this' keyword outside of a class");

            compiler.NamedVariable("this", canAssign);
        }
    }
}
