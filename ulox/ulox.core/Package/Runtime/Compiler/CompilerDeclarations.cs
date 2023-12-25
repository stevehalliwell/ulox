using System.Collections.Generic;

namespace ULox
{
    public static class CompilerDeclarations
    {
        public static void FunctionDeclaration(Compiler compiler)
        {
            CompilerExpressions.InnerFunctionDeclaration(compiler, true);
        }

        public static void VarDeclaration(Compiler compiler)
        {
            if (compiler.TokenIterator.Match(TokenType.OPEN_PAREN))
                MultiVarAssignToReturns(compiler);
            else
                PlainVarDeclare(compiler);

            compiler.ConsumeEndStatement();
        }

        private static void PlainVarDeclare(Compiler compiler)
        {
            do
            {
                var id = compiler.ParseVariable("Expect variable name");

                if (compiler.TokenIterator.Match(TokenType.ASSIGN))
                    compiler.Expression();
                else
                    compiler.EmitNULL();

                compiler.DefineVariable(id);
            } while (compiler.TokenIterator.Match(TokenType.COMMA));
        }

        private static void MultiVarAssignToReturns(Compiler compiler)
        {
            var varNames = new List<string>();
            do
            {
                compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect identifier within multivar declaration.");
                varNames.Add((string)compiler.TokenIterator.PreviousToken.Literal);
            } while (compiler.TokenIterator.Match(TokenType.COMMA));

            compiler.TokenIterator.Consume(TokenType.CLOSE_PAREN, "Expect ')' to end a multivar declaration.");
            compiler.TokenIterator.Consume(TokenType.ASSIGN, "Expect '=' after multivar declaration.");

            //mark stack start
            compiler.EmitPacket(new ByteCodePacket(OpCode.MULTI_VAR, true));

            compiler.Expression();

            compiler.EmitPacket(new ByteCodePacket(OpCode.MULTI_VAR, false));

            compiler.EmitPacket(new ByteCodePacket(new ByteCodePacket.PushValueDetails(varNames.Count)));
            compiler.EmitPacket(new ByteCodePacket(OpCode.VALIDATE, ValidateOp.MultiReturnMatches));

            if (compiler.CurrentCompilerState.scopeDepth == 0)
            {
                for (int i = varNames.Count - 1; i >= 0; i--)
                {
                    var varName = varNames[i];
                    compiler.DeclareAndDefineCustomVariable(varName);
                }
            }
            else
            {
                for (int i = 0; i < varNames.Count; i++)
                {
                    var varName = varNames[i];
                    compiler.DeclareAndDefineCustomVariable(varName);
                }
            }
        }
    }
}
