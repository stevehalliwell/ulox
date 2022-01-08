using System.Collections.Generic;

namespace ULox
{
    public class VarDeclarationCompilette : ICompilette
    {
        public TokenType Match => TokenType.VAR;

        public static void VarDeclaration(CompilerBase compiler)
        {
            if (compiler.Match(TokenType.OPEN_PAREN))
                MultiVarAssignToReturns(compiler);
            else
                PlainVarDeclare(compiler);

            //todo repeated
            compiler.Consume(TokenType.END_STATEMENT, "Expect ; after variable declaration.");
        }

        private static void PlainVarDeclare(CompilerBase compiler)
        {
            do
            {
                var global = compiler.ParseVariable("Expect variable name");

                if (compiler.Match(TokenType.ASSIGN))
                    compiler.Expression();
                else
                    compiler.EmitOpCode(OpCode.NULL);

                compiler.DefineVariable(global);
            } while (compiler.Match(TokenType.COMMA));
        }

        private static void MultiVarAssignToReturns(CompilerBase compiler)
        {
            var varNames = new List<string>();
            do
            {
                compiler.Consume(TokenType.IDENTIFIER, "Expect identifier within multivar declaration.");
                varNames.Add((string)compiler.PreviousToken.Literal);
            } while (compiler.Match(TokenType.COMMA));

            compiler.Consume(TokenType.CLOSE_PAREN, "Expect ')' to end a multivar declaration.");
            compiler.Consume(TokenType.ASSIGN, "Expect '=' after multivar declaration.");

            //mark stack start
            compiler.EmitOpAndBytes(OpCode.RETURN, (byte)ReturnMode.MarkMultiReturnAssignStart);

            compiler.Expression();

            compiler.EmitOpAndBytes(OpCode.RETURN, (byte)ReturnMode.MarkMultiReturnAssignEnd);

            compiler.EmitOpAndBytes(OpCode.PUSH_BYTE, (byte)varNames.Count);
            compiler.EmitOpAndBytes(OpCode.VALIDATE, (byte)ValidateOp.MultiReturnMatches);

            //we don't really want to reverse these, as we want things kike (a,b) = fun return (1,2,3); ends up with 1,2
            for (int i = 0; i < varNames.Count; i++)
            {
                var varName = varNames[i];
                //do equiv of ParseVariable, DefineVariable
                compiler.CurrentCompilerState.DeclareVariableByName(varName);
                compiler.CurrentCompilerState.MarkInitialised();
                var id = compiler.AddCustomStringConstant(varName);
                compiler.DefineVariable(id);
            }
        }

        public void Process(CompilerBase compiler)
            => VarDeclaration(compiler);
    }
}
