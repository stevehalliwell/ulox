using System.Collections.Generic;

namespace ULox
{
    public class TypeMixinCompilette : ITypeBodyCompilette
    {
        private Stack<string> _mixinNames = new Stack<string>();
        private ClassCompilette _classCompilette;

        public TypeMixinCompilette(ClassCompilette classCompilette)
        {
            _classCompilette = classCompilette;
        }

        public TokenType Match => TokenType.MIXIN;
        public TypeCompiletteStage Stage => TypeCompiletteStage.Mixin;

        public void End()
        {
        }

        public void PostBody(CompilerBase compiler)
        {
            //dump all mixins after everything else so we don't have to fight regular class setup process in vm
            while (_mixinNames.Count > 0)
            {
                var mixinName = _mixinNames.Pop();
                compiler.NamedVariable(mixinName, false);
                compiler.NamedVariable(_classCompilette.CurrentClassName, false);
                compiler.EmitOpAndBytes(OpCode.MIXIN);
            }
        }

        public void PreBody(CompilerBase compiler)
        {
        }

        public void Process(CompilerBase compiler)
        {
            do
            {
                compiler.Consume(TokenType.IDENTIFIER, "Expect identifier after mixin into class.");
                _mixinNames.Push(compiler.PreviousToken.Literal as string);
            } while (compiler.Match(TokenType.COMMA));

            //TODO many methods trail with end statement, DRY.
            compiler.Consume(TokenType.END_STATEMENT, "Expect ; after mixin declaration.");
        }

        public void Start()
        {
            _mixinNames.Clear();
        }
    }
}
