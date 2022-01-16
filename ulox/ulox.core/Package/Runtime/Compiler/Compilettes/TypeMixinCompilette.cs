using System.Collections.Generic;

namespace ULox
{
    public class TypeMixinCompilette : ITypeBodyCompilette
    {
        private Stack<string> _mixinNames = new Stack<string>();
        private TypeCompilette _typeCompilette;

        public TypeMixinCompilette(TypeCompilette typeCompilette)
            => _typeCompilette = typeCompilette;

        public TokenType Match 
            => TokenType.MIXIN;
        public TypeCompiletteStage Stage 
            => TypeCompiletteStage.Mixin;

        public void End() { }

        public void PostBody(CompilerBase compiler)
        {
            //dump all mixins after everything else so we don't have to fight regular class setup process in vm
            while (_mixinNames.Count > 0)
            {
                var mixinName = _mixinNames.Pop();
                compiler.NamedVariable(mixinName, false);
                compiler.NamedVariable(_typeCompilette.CurrentTypeName, false);
                compiler.EmitOpAndBytes(OpCode.MIXIN);
            }
        }

        public void PreBody(CompilerBase compiler) { }

        public void Process(CompilerBase compiler)
        {
            do
            {
                compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect identifier after mixin into class.");
                _mixinNames.Push(compiler.TokenIterator.PreviousToken.Literal as string);
            } while (compiler.TokenIterator.Match(TokenType.COMMA));

            compiler.ConsumeEndStatement("mixin declaration");
        }

        public void Start()
            => _mixinNames.Clear();
    }
}
