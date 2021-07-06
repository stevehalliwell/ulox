using System.Collections.Generic;

namespace ULox
{
    public class Compiler : CompilerBase
    {


        protected override void GenerateDeclarationLookup()
        {
            var decl = new List<Compilette>()
            {
                new Compilette(TokenType.TEST, TestDeclaration),
                new Compilette(TokenType.CLASS, ClassDeclaration),
                new Compilette(TokenType.FUNCTION, FunctionDeclaration),
                new Compilette(TokenType.VAR, VarDeclaration),
            };

            foreach (var item in decl)
                declarationCompilettes[item.Match] = item;
        }

        protected override void GenerateStatementLookup()
        {
            var statement = new List<Compilette>()
            {
                new Compilette(TokenType.IF, IfStatement),
                new Compilette(TokenType.RETURN, ReturnStatement),
                new Compilette(TokenType.YIELD, YieldStatement),
                new Compilette(TokenType.BREAK, BreakStatement),
                new Compilette(TokenType.CONTINUE, ContinueStatement),
                new Compilette(TokenType.LOOP, LoopStatement),
                new Compilette(TokenType.WHILE, WhileStatement),
                new Compilette(TokenType.FOR, ForStatement),
                new Compilette(TokenType.OPEN_BRACE, BlockStatement),
                new Compilette(TokenType.THROW, ThrowStatement),
            };

            foreach (var item in statement)
                statementCompilettes[item.Match] = item;
            }
    }
}
