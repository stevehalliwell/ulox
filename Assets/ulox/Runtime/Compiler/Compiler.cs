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


        protected override void GenerateParseRules()
        {
            rules = new ParseRule[System.Enum.GetNames(typeof(TokenType)).Length];

            for (int i = 0; i < rules.Length; i++)
            {
                rules[i] = new ParseRule(null, null, Precedence.None);
            }

            rules[(int)TokenType.MINUS] = new ParseRule(Unary, Binary, Precedence.Term);
            rules[(int)TokenType.PLUS] = new ParseRule(null, Binary, Precedence.Term);
            rules[(int)TokenType.SLASH] = new ParseRule(null, Binary, Precedence.Factor);
            rules[(int)TokenType.STAR] = new ParseRule(null, Binary, Precedence.Factor);
            rules[(int)TokenType.BANG] = new ParseRule(Unary, null, Precedence.None);
            rules[(int)TokenType.INT] = new ParseRule(Number, null, Precedence.None);
            rules[(int)TokenType.FLOAT] = new ParseRule(Number, null, Precedence.None);
            rules[(int)TokenType.TRUE] = new ParseRule(Literal, null, Precedence.None);
            rules[(int)TokenType.FALSE] = new ParseRule(Literal, null, Precedence.None);
            rules[(int)TokenType.NULL] = new ParseRule(Literal, null, Precedence.None);
            rules[(int)TokenType.BANG_EQUAL] = new ParseRule(null, Binary, Precedence.Equality);
            rules[(int)TokenType.EQUALITY] = new ParseRule(null, Binary, Precedence.Equality);
            rules[(int)TokenType.LESS] = new ParseRule(null, Binary, Precedence.Comparison);
            rules[(int)TokenType.LESS_EQUAL] = new ParseRule(null, Binary, Precedence.Comparison);
            rules[(int)TokenType.GREATER] = new ParseRule(null, Binary, Precedence.Comparison);
            rules[(int)TokenType.GREATER_EQUAL] = new ParseRule(null, Binary, Precedence.Comparison);
            rules[(int)TokenType.STRING] = new ParseRule(String, null, Precedence.None);
            rules[(int)TokenType.IDENTIFIER] = new ParseRule(Variable, null, Precedence.None);
            rules[(int)TokenType.AND] = new ParseRule(null, And, Precedence.And);
            rules[(int)TokenType.OR] = new ParseRule(null, Or, Precedence.Or);
            rules[(int)TokenType.OPEN_PAREN] = new ParseRule(Grouping, Call, Precedence.Call);
            rules[(int)TokenType.DOT] = new ParseRule(null, Dot, Precedence.Call);
            rules[(int)TokenType.THIS] = new ParseRule(This, null, Precedence.None);
            rules[(int)TokenType.SUPER] = new ParseRule(Super, null, Precedence.None);
        }
    }
}
