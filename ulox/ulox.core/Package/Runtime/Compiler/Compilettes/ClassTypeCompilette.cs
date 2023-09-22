using System.Collections.Generic;

namespace ULox
{
    public sealed class ClassTypeCompilette : TypeCompilette
    {
        public static readonly HashedString InitMethodName = new HashedString("init");
        public static readonly HashedString ThisName = new HashedString("this");

        private readonly Dictionary<TokenType, ITypeBodyCompilette> _innerDeclarationCompilettes = new Dictionary<TokenType, ITypeBodyCompilette>();
        private ITypeBodyCompilette _bodyCompiletteFallback;

        public override TokenType MatchingToken => TokenType.CLASS;

        public override UserType UserType => UserType.Class;

        public ClassTypeCompilette()
        {
            AddInnerDeclarationCompilette(new TypeStaticElementCompilette());
            AddInnerDeclarationCompilette(new TypeInitCompilette());
            AddInnerDeclarationCompilette(new TypeMethodCompilette());
            AddInnerDeclarationCompilette(new TypeSignsCompilette(this));
            AddInnerDeclarationCompilette(new TypeMixinCompilette(this));
            AddInnerDeclarationCompilette(new TypeInstancePropertyCompilette(this));
        }

        public void This(Compiler compiler, bool canAssign)
        {
            if (CurrentTypeName == null)
                compiler.ThrowCompilerException("Cannot use the 'this' keyword outside of a class");

            compiler.NamedVariable(ThisName.String, canAssign);
        }

        private void AddInnerDeclarationCompilette(ITypeBodyCompilette compilette)
        {
            _innerDeclarationCompilettes[compilette.MatchingToken] = compilette;
            if (compilette.MatchingToken == TokenType.NONE)
                _bodyCompiletteFallback = compilette;
        }

        protected override void InnerBodyElement(Compiler compiler)
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

        private void ValidStage(Compiler compiler, TypeCompiletteStage stage)
        {
            if (Stage > stage)
                compiler.ThrowCompilerException($"Stage out of order. Type '{CurrentTypeName}' is at stage '{Stage}' has encountered a late '{stage}' stage element");

            Stage = stage;
        }
    }
}
