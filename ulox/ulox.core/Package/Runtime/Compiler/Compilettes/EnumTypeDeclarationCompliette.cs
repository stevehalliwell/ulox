using System.Collections.Generic;

namespace ULox
{
    //todo can enums also be made entirely at compile time? or become sugar on a readonly dynamic?
    public sealed class EnumTypeDeclarationCompliette : TypeCompilette
    {
        private enum Mode
        {
            Unknown,
            Manual,
            Auto,
        }

        private readonly List<string> _enumKeys = new List<string>();
        private double _previousNumber = -1;
        private Mode _mode = Mode.Unknown;

        public override TokenType MatchingToken => TokenType.ENUM;

        public override UserType UserType => UserType.Enum;

        public override bool EmitClosureCallAtEnd => true;

        public EnumTypeDeclarationCompliette()
        {
            IsReadOnlyAtEnd = true;
        }

        protected override void Start()
        {
            _enumKeys.Clear();
            _previousNumber = -1;
            _mode = Mode.Unknown;
        }

        protected override void InnerBodyElement(Compiler compiler)
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
                    SetMode(compiler, Mode.Manual);
                    compiler.Expression();
                }
                else
                {
                    SetMode(compiler, Mode.Auto);
                    compiler.EmitPushValue((byte)++_previousNumber);
                }


                compiler.EmitPacket(new ByteCodePacket(OpCode.GET_LOCAL, 1));//get class or inst this on the stack
                compiler.EmitPacket(new ByteCodePacket(OpCode.ENUM_VALUE));

                compiler.TokenIterator.Match(TokenType.COMMA);
            } while (compiler.TokenIterator.Match(TokenType.IDENTIFIER));
        }

        private void SetMode(Compiler compiler, Mode mode)
        {
            if (_mode != Mode.Unknown)
            {
                if (mode != _mode)
                    compiler.ThrowCompilerException($"Cannot mix and match enum assignment modes. Current mode is '{_mode}' but encounted a '{mode}'");
            }
            _mode = mode;
        }
    }
}
