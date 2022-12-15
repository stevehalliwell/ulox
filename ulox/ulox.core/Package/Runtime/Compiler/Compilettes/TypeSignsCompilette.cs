using System.Collections.Generic;

namespace ULox
{
    public class TypeSignsCompilette : ITypeBodyCompilette
    {
        private List<string> _contractNames = new List<string>();
        private TypeCompilette _typeCompilette;

        public TokenType Match
            => TokenType.SIGNS;
        public TypeCompiletteStage Stage
            => TypeCompiletteStage.Signs;

        public void Start(TypeCompilette typeCompilette)
        {
            _typeCompilette = typeCompilette;
            _contractNames.Clear();
        }

        public void Process(Compiler compiler)
        {
            do
            {
                compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect identifier after signs into class.");
                _contractNames.Add(compiler.TokenIterator.PreviousToken.Literal as string);
            } while (compiler.TokenIterator.Match(TokenType.COMMA));

            compiler.ConsumeEndStatement("signs declaration");
        }

        public void PostBody(Compiler compiler)
        {
            foreach (var contractName in _contractNames)
            {
                compiler.NamedVariable(_typeCompilette.CurrentTypeName, false);
                compiler.NamedVariable(contractName, false);
                compiler.EmitOpCode(OpCode.SIGNS);
            }
        }
    }
}
