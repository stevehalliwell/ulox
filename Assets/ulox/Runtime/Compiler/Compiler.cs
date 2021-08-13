using System.Collections.Generic;

namespace ULox
{
    public class Compiler : CompilerBase
    {
        public Compiler()
        {
            this.SetupSimpleCompiler();
            var testcaseCompilette = new TestcaseCompillette();
            var testdec = new TestDeclarationCompilette();
            testcaseCompilette.SetTestDeclarationCompilette(testdec);
            this.AddDeclarationCompilettes(
                testdec,
                new ClassCompilette(),
                testcaseCompilette);

            this.SetPrattRules(
                (TokenType.DOT, new ParseRule(null, this.Dot, Precedence.Call)),
                (TokenType.THIS, new ParseRule(This, null, Precedence.None)),
                (TokenType.SUPER, new ParseRule(Super, null, Precedence.None)));
        }

        #region Expressions
        protected void This(bool canAssign)
        {
            if (GetEnclosingClass() == null)
                throw new CompilerException("Cannot use this outside of a class declaration.");

            Variable(false);
        }

        protected void Super(bool canAssign)
        {
            if (GetEnclosingClass() == null)
                throw new CompilerException("Cannot use super outside a class.");
            //todo cannot use outisde a class without a super

            Consume(TokenType.DOT, "Expect '.' after a super.");
            Consume(TokenType.IDENTIFIER, "Expect superclass method name.");
            var nameID = AddStringConstant();

            NamedVariable("this", false);
            if (Match(TokenType.OPEN_PAREN))
            {
                byte argCount = ArgumentList();
                NamedVariable("super", false);
                EmitOpAndBytes(OpCode.SUPER_INVOKE, nameID);
                EmitBytes(argCount);
            }
            else
            {
                NamedVariable("super", false);
                EmitOpAndBytes(OpCode.GET_SUPER, nameID);
            }
        }
        #endregion Expressions
    }
}
