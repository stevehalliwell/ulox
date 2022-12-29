﻿using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ULox
{
    public class VarDeclarationCompilette : ICompilette
    {
        public TokenType Match => TokenType.VAR;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void VarDeclaration(Compiler compiler)
        {
            if (compiler.TokenIterator.Match(TokenType.OPEN_PAREN))
                MultiVarAssignToReturns(compiler);
            else
                PlainVarDeclare(compiler);

            compiler.ConsumeEndStatement();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void PlainVarDeclare(Compiler compiler)
        {
            do
            {
                var global = compiler.ParseVariable("Expect variable name");

                if (compiler.TokenIterator.Match(TokenType.ASSIGN))
                    compiler.Expression();
                else
                    compiler.EmitNULL();

                compiler.DefineVariable(global);
            } while (compiler.TokenIterator.Match(TokenType.COMMA));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            compiler.EmitReturnPacket(ReturnMode.MarkMultiReturnAssignStart);

            compiler.Expression();

            compiler.EmitReturnPacket(ReturnMode.MarkMultiReturnAssignEnd);

            compiler.EmitOpAndBytes(OpCode.PUSH_BYTE, (byte)varNames.Count);
            compiler.EmitOpAndBytes(OpCode.VALIDATE, (byte)ValidateOp.MultiReturnMatches);

            //we don't really want to reverse these, as we want things kike (a,b) = fun return (1,2,3); ends up with 1,2
            for (int i = 0; i < varNames.Count; i++)
            {
                var varName = varNames[i];
                compiler.DeclareAndDefineCustomVariable(varName);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Process(Compiler compiler)
            => VarDeclaration(compiler);
    }
}
