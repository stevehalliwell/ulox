using System.Collections.Generic;
using System.Linq;

namespace ULox
{
    public class TypeEnumValueCompilette : ITypeBodyCompilette
    {
        private List<(string, object)> _enumKVs = new List<(string, object)>();
        private TypeCompilette _typeCompilette;
        private double _previousNumber = -1;

        public TokenType Match
            => TokenType.NONE;
        public TypeCompiletteStage Stage
            => TypeCompiletteStage.Var;

        public void Start(TypeCompilette typeCompilette)
        {
            _typeCompilette = typeCompilette;
            _enumKVs.Clear();
            _previousNumber = -1;
        }

        public void Process(Compiler compiler)
        {
            compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect identifier next in enum declare.");
            
            do
            {
                var enumKey = compiler.TokenIterator.PreviousToken.Literal as string;

                if (_enumKVs.Any(x => x.Item1 == enumKey))
                    compiler.ThrowCompilerException($"Duplicate Enum Key '{enumKey}'");

                if (compiler.TokenIterator.Match(TokenType.ASSIGN))
                {
                    compiler.TokenIterator.Consume(TokenType.NUMBER, "Expect number after '='.");
                    _previousNumber = (double)compiler.TokenIterator.PreviousToken.Literal;
                    _enumKVs.Add((enumKey, _previousNumber));
                }
                else
                {
                    _previousNumber++;
                    _enumKVs.Add((enumKey, _previousNumber));
                }
                compiler.TokenIterator.Match(TokenType.COMMA);
            } while (compiler.TokenIterator.Match(TokenType.IDENTIFIER));
        }

        public void PostBody(Compiler compiler)
        {
            //dump all mixins after everything else so we don't have to fight regular class setup process in vm
            for (int i = 0; i < _enumKVs.Count; i++)
            {
                var (k, v) = _enumKVs[i];
                compiler.DoNumberConstant((double)v);
                compiler.AddConstantAndWriteOp(Value.New(k));
                compiler.NamedVariable(_typeCompilette.CurrentTypeName, false);
                compiler.EmitOpCode(OpCode.ENUM_VALUE);
            }
        }
    }
}
