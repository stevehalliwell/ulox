using System.Collections.Generic;
using System.Linq;

namespace ULox
{
    public abstract class TypeCompilette : ICompilette
    {
        public abstract TokenType Match { get; }

        public abstract void Process(CompilerBase compiler);

        public static readonly HashedString InitMethodName = new HashedString("init");

        protected Dictionary<TokenType, ITypeBodyCompilette> _innerDeclarationCompilettes = new Dictionary<TokenType, ITypeBodyCompilette>();
        private ITypeBodyCompilette _bodyCompiletteFallback;

        protected TypeCompiletteStage _stage = TypeCompiletteStage.Invalid;
        public string CurrentTypeName { get; protected set; }
        private short _initChainInstruction;
        public short InitChainInstruction => _initChainInstruction;
        private bool _hasSuper;
        protected ITypeBodyCompilette[] _stageOrderedBodyCompilettes;

        protected void GenerateCompiletteByStageArray()
        {
            _stageOrderedBodyCompilettes = _innerDeclarationCompilettes.Values
                .OrderBy(x => x.Stage)
                .ToArray();
        }

        public void AddInnerDeclarationCompilette(ITypeBodyCompilette compilette)
        {
            _innerDeclarationCompilettes[compilette.Match] = compilette;
            if (compilette.Match == TokenType.NONE)
                _bodyCompiletteFallback = compilette;
        }

        protected void DoEndType(CompilerBase compiler)
        {
            compiler.TokenIterator.Consume(TokenType.CLOSE_BRACE, "Expect '}' after class body.");
            compiler.EmitOpCode(OpCode.POP);

            if (_hasSuper)
            {
                compiler.EndScope();
            }
        }

        protected void DoEndDeclareType(CompilerBase compiler)
        {
            compiler.NamedVariable(CurrentTypeName, false);
            compiler.TokenIterator.Consume(TokenType.OPEN_BRACE, "Expect '{' before type body.");
        }

        protected void DoBeginDeclareType(CompilerBase compiler)
        {
            _stage = TypeCompiletteStage.Begin;
            compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect type name.");
            CurrentTypeName = (string)compiler.TokenIterator.PreviousToken.Literal;
            byte nameConstant = compiler.AddStringConstant();
            compiler.DeclareVariable();
            EmitClassOp(compiler, nameConstant);
            compiler.DefineVariable(nameConstant);
        }

        protected void DoClassBody(CompilerBase compiler)
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
            CompilerBase compiler,
            byte nameConstant)
        {
            compiler.EmitOpAndBytes(OpCode.CLASS, nameConstant);
            _initChainInstruction = (short)compiler.CurrentChunk.Instructions.Count;
            compiler.EmitUShort(0);
        }

        protected void DoDeclareLineInher(CompilerBase compiler)
        {
            _hasSuper = false;
            if (!compiler.TokenIterator.Match(TokenType.LESS)) return;

            compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect superclass name.");
            var name = (string)compiler.TokenIterator.PreviousToken.Literal;
            compiler.NamedVariable(name, false);
            if (CurrentTypeName == (string)compiler.TokenIterator.PreviousToken.Literal)
                throw new CompilerException($"A class cannot inher from itself. '{CurrentTypeName}' inherits from itself, not allowed.");

            compiler.BeginScope();
            compiler.CurrentCompilerState.AddLocal("super");
            compiler.DefineVariable(0);

            compiler.NamedVariable(CurrentTypeName, false);
            compiler.EmitOpCode(OpCode.INHERIT);
            _hasSuper = true;
        }
    }
}
