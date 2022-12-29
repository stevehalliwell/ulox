using System.Collections.Generic;

namespace ULox
{
    public class TypeEnumValueCompilette : ITypeBodyCompilette
    {
        private readonly List<string> _enumKeys = new List<string>();
        private double _previousNumber = -1;
        private bool _manualAssign = false;

        public TokenType Match
            => TokenType.NONE;
        public TypeCompiletteStage Stage
            => TypeCompiletteStage.Var;

        public void Start(TypeCompilette typeCompilette)
        {
            _enumKeys.Clear();
            _previousNumber = -1;
            _manualAssign = false;
        }

        public void Process(Compiler compiler)
        {
            compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect identifier next in enum declare.");
            
            do
            {
                var enumKey = compiler.TokenIterator.PreviousToken.Literal as string;

                if (_enumKeys.Contains(enumKey))
                    compiler.ThrowCompilerException($"Duplicate Enum Key '{enumKey}'");
                
                _enumKeys.Add(enumKey);

                compiler.AddConstantAndWriteOp(Value.New(enumKey));

                if (compiler.TokenIterator.Match(TokenType.ASSIGN))
                {
                    compiler.Expression(); 
                    _manualAssign = true;
                }
                else
                {
                    if(_manualAssign)
                        compiler.ThrowCompilerException($"Enum Key '{enumKey}' must be assigned a value. Cannot mix and match.");
                    
                    compiler.DoNumberConstant(++_previousNumber);
                }


                compiler.EmitOpAndBytes(OpCode.GET_LOCAL, 1);//get class or inst this on the stack
                compiler.EmitPacket(OpCode.ENUM_VALUE);
                
                compiler.TokenIterator.Match(TokenType.COMMA);
            } while (compiler.TokenIterator.Match(TokenType.IDENTIFIER));
        }
    }
}
