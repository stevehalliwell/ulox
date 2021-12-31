using System.Collections.Generic;
using System.Linq;

namespace ULox
{
    public class ClassCompilette : ICompilette
    {
        public static readonly HashedString InitMethodName = new HashedString("init");

        protected Dictionary<TokenType, ITypeBodyCompilette> _innerDeclarationCompilettes = new Dictionary<TokenType, ITypeBodyCompilette>();
        private ITypeBodyCompilette _bodyCompiletteFallback;

        protected TypeCompiletteStage _stage = TypeCompiletteStage.Invalid;
        public string CurrentClassName { get; protected set; }
        private short _initChainInstruction;
        public short InitChainInstruction => _initChainInstruction;
        private bool _hasSuper;
        private readonly ITypeBodyCompilette[] _stageOrderedBodyCompilettes;

        public ClassCompilette()
        {
            AddInnerDeclarationCompilette(new TypeStaticElementCompilette());
            AddInnerDeclarationCompilette(new TypeInitCompilette());
            AddInnerDeclarationCompilette(new TypeMethodCompilette());
            AddInnerDeclarationCompilette(new TypeMixinCompilette(this));
            AddInnerDeclarationCompilette(new TypePropertyCompilette(this));

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

        public virtual TokenType Match => TokenType.CLASS;

        public virtual void Process(CompilerBase compiler)
        {
            ClassDeclaration(compiler);
        }

        private void ClassDeclaration(CompilerBase compiler)
        {
            foreach (var bodyCompilette in _stageOrderedBodyCompilettes)
                bodyCompilette.Start();

            DoBeginDeclareType(compiler, out var compState);
            DoDeclareLineInher(compiler, compState);
            DoEndDeclareType(compiler);

            foreach (var bodyCompilette in _stageOrderedBodyCompilettes)
                bodyCompilette.PreBody(compiler);

            DoClassBody(compiler);

            foreach (var bodyCompilette in _stageOrderedBodyCompilettes)
                bodyCompilette.PostBody(compiler);

            DoEndType(compiler);

            foreach (var bodyCompilette in _stageOrderedBodyCompilettes)
                bodyCompilette.End();

            CurrentClassName = null;
        }

        protected void DoEndType(CompilerBase compiler)
        {
            compiler.Consume(TokenType.CLOSE_BRACE, "Expect '}' after class body.");
            compiler.EmitOpCode(OpCode.POP);

            if (_hasSuper)
            {
                compiler.EndScope();
            }
        }

        protected void DoEndDeclareType(CompilerBase compiler)
        {
            compiler.NamedVariable(CurrentClassName, false);
            compiler.Consume(TokenType.OPEN_BRACE, "Expect '{' before type body.");
        }

        protected void DoBeginDeclareType(CompilerBase compiler, out CompilerState compState)
        {
            _stage = TypeCompiletteStage.Begin;
            compiler.Consume(TokenType.IDENTIFIER, "Expect type name.");
            CurrentClassName = (string)compiler.PreviousToken.Literal;
            compState = compiler.CurrentCompilerState;
            byte nameConstant = compiler.AddStringConstant();
            compiler.DeclareVariable();
            EmitClassOp(compiler, nameConstant);
            compiler.DefineVariable(nameConstant);
        }

        protected void DoClassBody(CompilerBase compiler)
        {
            while (!compiler.Check(TokenType.CLOSE_BRACE) && !compiler.Check(TokenType.EOF))
            {
                var compilette = _bodyCompiletteFallback;
                if (_innerDeclarationCompilettes.TryGetValue(compiler.CurrentToken.TokenType, out var matchingCompilette))
                {
                    compiler.Advance();
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
                throw new CompilerException($"Type '{CurrentClassName}', encountered element of stage '{stage}' too late, type is at stage '{_stage}'. This is not allowed.");
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

        protected void DoDeclareLineInher(CompilerBase compiler, CompilerState compState)
        {
            _hasSuper = false;
            if (!compiler.Match(TokenType.LESS)) return;

            compiler.Consume(TokenType.IDENTIFIER, "Expect superclass name.");
            compiler.NamedVariableFromPreviousToken(false);
            if (CurrentClassName == (string)compiler.PreviousToken.Literal)
                throw new CompilerException("A class cannot inher from itself.");

            compiler.BeginScope();
            compiler.AddLocal(compState, "super");
            compiler.DefineVariable(0);

            compiler.NamedVariable(CurrentClassName, false);
            compiler.EmitOpCode(OpCode.INHERIT);
            _hasSuper = true;
        }
    }
}
