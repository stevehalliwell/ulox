using System;
using System.Collections.Generic;
using System.Linq;

namespace ULox
{
    public enum UserType : byte
    {
        Native,
        Data,
        Class,
    }

    public class TypeCompilette : ICompilette
    {
        protected TypeCompilette() { }

        public static TypeCompilette CreateClassCompilette()
        {
            var compilette = new TypeCompilette();
            compilette.AddInnerDeclarationCompilette(new TypeStaticElementCompilette());
            compilette.AddInnerDeclarationCompilette(new TypeInitCompilette());
            compilette.AddInnerDeclarationCompilette(new TypeMethodCompilette());
            compilette.AddInnerDeclarationCompilette(new TypeSignsCompilette());
            compilette.AddInnerDeclarationCompilette(new TypeMixinCompilette());
            compilette.AddInnerDeclarationCompilette(TypePropertyCompilette.CreateForClass());
            compilette.GenerateCompiletteByStageArray();
            compilette.UserType = UserType.Class;
            compilette.Match = TokenType.CLASS;
            return compilette;
        }

        public static TypeCompilette CreateDateCompilette()
        {
            var compilette = new TypeCompilette();
            compilette.AddInnerDeclarationCompilette(new TypeSignsCompilette());
            compilette.AddInnerDeclarationCompilette(new TypeMixinCompilette());
            compilette.AddInnerDeclarationCompilette(TypePropertyCompilette.CreateForData());
            compilette.GenerateCompiletteByStageArray();
            compilette.UserType = UserType.Data;
            compilette.Match = TokenType.DATA;
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

        private static readonly TypeCompiletteStage[] PostBodyCompileStageOrder = new[]
        {
            TypeCompiletteStage.Invalid,
            TypeCompiletteStage.Begin,
            TypeCompiletteStage.Static,
            TypeCompiletteStage.Mixin,
            TypeCompiletteStage.Var,
            TypeCompiletteStage.Init,
            TypeCompiletteStage.Method,
            TypeCompiletteStage.Signs,
            TypeCompiletteStage.Complete
        };

        public TokenType Match { get; private set; }

        public static readonly HashedString InitMethodName = new HashedString("init");

        protected Dictionary<TokenType, ITypeBodyCompilette> _innerDeclarationCompilettes = new Dictionary<TokenType, ITypeBodyCompilette>();
        private ITypeBodyCompilette _bodyCompiletteFallback;

        protected TypeCompiletteStage _stage = TypeCompiletteStage.Invalid;
        public string CurrentTypeName { get; protected set; }
        private short _initChainInstruction;
        public short InitChainInstruction => _initChainInstruction;
        protected ITypeBodyCompilette[] BodyCompilettesProcessingOrdered;
        protected ITypeBodyCompilette[] BodyCompilettesPostBodyOrdered;

        public void AddInnerDeclarationCompilette(ITypeBodyCompilette compilette)
        {
            _innerDeclarationCompilettes[compilette.Match] = compilette;
            if (compilette.Match == TokenType.NONE)
                _bodyCompiletteFallback = compilette;
        }

        public void CName(Compiler compiler, bool canAssign)
        {
            var cname = CurrentTypeName;
            compiler.AddConstantAndWriteOp(Value.New(cname));
        }

        public UserType UserType { get; private set; }

        public virtual void Process(Compiler compiler)
            => StageBasedDeclaration(compiler);

        protected void StageBasedDeclaration(Compiler compiler)
        {
            foreach (var bodyCompilette in BodyCompilettesProcessingOrdered)
                bodyCompilette.Start(this);

            DoBeginDeclareType(compiler);
            DoEndDeclareType(compiler);

            foreach (var bodyCompilette in BodyCompilettesProcessingOrdered)
                bodyCompilette.PreBody(compiler);

            DoClassBody(compiler);

            foreach (var bodyCompilette in BodyCompilettesPostBodyOrdered)
                bodyCompilette.PostBody(compiler);

            DoEndType(compiler);

            foreach (var bodyCompilette in BodyCompilettesProcessingOrdered)
                bodyCompilette.End();

            CurrentTypeName = null;
        }

        protected void GenerateCompiletteByStageArray()
        {
            BodyCompilettesProcessingOrdered = _innerDeclarationCompilettes.Values
                .OrderBy(x => Array.IndexOf(BodyCompileStageOrder, x.Stage))
                .ToArray();
            BodyCompilettesPostBodyOrdered = _innerDeclarationCompilettes.Values
                .OrderBy(x => Array.IndexOf(PostBodyCompileStageOrder, x.Stage))
                .ToArray();
        }
        
        protected void DoEndType(Compiler compiler)
        {
            compiler.TokenIterator.Consume(TokenType.CLOSE_BRACE, "Expect '}' after class body.");
            compiler.EmitOpCode(OpCode.FREEZE);
        }

        protected void DoEndDeclareType(Compiler compiler)
        {
            compiler.NamedVariable(CurrentTypeName, false);
            compiler.TokenIterator.Consume(TokenType.OPEN_BRACE, "Expect '{' before type body.");
        }

        protected void DoBeginDeclareType(Compiler compiler)
        {
            _stage = TypeCompiletteStage.Begin;
            compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect type name.");
            CurrentTypeName = (string)compiler.TokenIterator.PreviousToken.Literal;
            byte nameConstant = compiler.AddStringConstant();
            compiler.DeclareVariable();
            EmitUserTypeOpAndBytes(compiler, nameConstant);
            compiler.DefineVariable(nameConstant);
        }

        protected void DoClassBody(Compiler compiler)
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

        protected void ValidStage(Compiler compiler, TypeCompiletteStage stage)
        {
            if (_stage > stage)
            {
                compiler.ThrowCompilerException($"Stage out of order. Type '{CurrentTypeName}' is at stage '{_stage}' has encountered a late '{stage}' stage element");
            }
            _stage = stage;
        }

        private void EmitUserTypeOpAndBytes(
            Compiler compiler,
            byte nameConstant)
        {
            compiler.EmitOpAndBytes(OpCode.TYPE, nameConstant, (byte)UserType);
            _initChainInstruction = (short)compiler.CurrentChunk.Instructions.Count;
            compiler.EmitUShort(0);
        }

        public void This(Compiler compiler, bool canAssign)
        {
            if (CurrentTypeName == null)
                compiler.ThrowCompilerException("Cannot use the 'this' keyword outside of a class");

            compiler.NamedVariable("this", canAssign);
        }
    }
}
