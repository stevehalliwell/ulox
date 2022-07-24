using System;
using System.Collections.Generic;
using System.Linq;

namespace ULox
{
    public abstract class TypeCompilette : ICompilette
    {
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
        
        public abstract TokenType Match { get; }

        public abstract void Process(Compiler compiler);

        public static readonly HashedString InitMethodName = new HashedString("init");

        protected Dictionary<TokenType, ITypeBodyCompilette> _innerDeclarationCompilettes = new Dictionary<TokenType, ITypeBodyCompilette>();
        private ITypeBodyCompilette _bodyCompiletteFallback;

        protected TypeCompiletteStage _stage = TypeCompiletteStage.Invalid;
        public string CurrentTypeName { get; protected set; }
        private short _initChainInstruction;
        public short InitChainInstruction => _initChainInstruction;
        protected ITypeBodyCompilette[] BodyCompilettesProcessingOrdered;
        protected ITypeBodyCompilette[] BodyCompilettesPostBodyOrdered;

        protected void GenerateCompiletteByStageArray()
        {
            BodyCompilettesProcessingOrdered = _innerDeclarationCompilettes.Values
                .OrderBy(x => Array.IndexOf(BodyCompileStageOrder,x.Stage))
                .ToArray();
            BodyCompilettesPostBodyOrdered = _innerDeclarationCompilettes.Values
                .OrderBy(x => Array.IndexOf(PostBodyCompileStageOrder, x.Stage))
                .ToArray();
        }

        public void AddInnerDeclarationCompilette(ITypeBodyCompilette compilette)
        {
            _innerDeclarationCompilettes[compilette.Match] = compilette;
            if (compilette.Match == TokenType.NONE)
                _bodyCompiletteFallback = compilette;
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
            EmitClassOp(compiler, nameConstant);
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

                ValidStage(compilette.Stage);
                compilette.Process(compiler);
            }

            _stage = TypeCompiletteStage.Complete;
        }

        protected void ValidStage(TypeCompiletteStage stage)
        {
            if (_stage > stage)
            {
                throw new CompilerException($"Type '{CurrentTypeName}', encountered element of stage '{stage}' too late, type is at stage '{_stage}'. This is not allowed.");
            }
            _stage = stage;
        }

        private void EmitClassOp(
            Compiler compiler,
            byte nameConstant)
        {
            compiler.EmitOpAndBytes(OpCode.CLASS, nameConstant);
            _initChainInstruction = (short)compiler.CurrentChunk.Instructions.Count;
            compiler.EmitUShort(0);
        }
    }
}
