using System.Collections.Generic;

namespace ULox
{
    public class TypeSignsCompilette : ITypeBodyCompilette
    {
        private TypeCompilette _typeCompilette;

        public TokenType Match
            => TokenType.SIGNS;
        public TypeCompiletteStage Stage
            => TypeCompiletteStage.Signs;

        public void Start(TypeCompilette typeCompilette)
        {
            _typeCompilette = typeCompilette;
        }

        public void Process(Compiler compiler)
        {
            var contractNames = new List<string>();
            do
            {
                compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect identifier after signs into class.");
                contractNames.Add(compiler.TokenIterator.PreviousToken.Literal as string);
            } while (compiler.TokenIterator.Match(TokenType.COMMA));

            compiler.ConsumeEndStatement("signs declaration");

            _typeCompilette.OnPostBody += x =>
            {
                foreach (var contractName in contractNames)
                {
                    compiler.NamedVariable(_typeCompilette.CurrentTypeName, false);
                    compiler.NamedVariable(contractName, false);
                    compiler.EmitOpCode(OpCode.SIGNS);
                }
            };
        }
    }
}
