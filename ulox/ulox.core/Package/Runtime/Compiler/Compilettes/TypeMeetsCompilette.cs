using System.Collections.Generic;

namespace ULox
{
    public class TypeMeetsCompilette : ITypeBodyCompilette
    {
        private List<string> _contractNames = new List<string>();
        private TypeCompilette _typeCompilette;

        public TokenType Match
            => TokenType.MEETS;
        public TypeCompiletteStage Stage
            => TypeCompiletteStage.Meets;

        public void Start(TypeCompilette typeCompilette)
            => _typeCompilette = typeCompilette;

        public void PreBody(Compiler compiler) { }

        public void Process(Compiler compiler)
        {
            do
            {
                compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect identifier after meets into class.");
                _contractNames.Add(compiler.TokenIterator.PreviousToken.Literal as string);
            } while (compiler.TokenIterator.Match(TokenType.COMMA));

            compiler.ConsumeEndStatement("meets declaration");
        }

        public void PostBody(Compiler compiler)
        {
            foreach (var contractName in _contractNames)
            {
                compiler.NamedVariable(_typeCompilette.CurrentTypeName, false);
                compiler.NamedVariable(contractName, false);
                compiler.EmitOpCode(OpCode.MEETS);
            }
        }

        public void End() { }
    }
}
