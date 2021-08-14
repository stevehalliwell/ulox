namespace ULox
{
    public static partial class CompilerBaseExt
    {
        public static void AddDeclarationCompilettes(this CompilerBase comp, params ICompilette[] compilettes)
        {
            foreach (var item in compilettes)
            {
                comp.AddDeclarationCompilette(item);
            }
        }

        public static void AddStatementCompilettes(this CompilerBase comp, params ICompilette[] compilettes)
        {
            foreach (var item in compilettes)
            {
                comp.AddStatementCompilette(item);
            }
        }

        public static void SetPrattRules(this CompilerBase comp, params (TokenType tt, ParseRule rule)[] rules)
        {
            foreach (var item in rules)
            {
                comp.SetPrattRule(item.tt, item.rule);
            }
        }

        public static void SetupSimpleCompiler(this CompilerBase comp)
        {
            comp.AddDeclarationCompilettes(
                new CompiletteAction(TokenType.FUNCTION, comp.FunctionDeclaration),
                new CompiletteAction(TokenType.VAR, comp.VarDeclaration));

            comp.AddStatementCompilettes(
                new CompiletteAction(TokenType.IF, comp.IfStatement),
                new CompiletteAction(TokenType.RETURN, comp.ReturnStatement),
                new CompiletteAction(TokenType.YIELD, comp.YieldStatement),
                new CompiletteAction(TokenType.BREAK, comp.BreakStatement),
                new CompiletteAction(TokenType.CONTINUE, comp.ContinueStatement),
                new CompiletteAction(TokenType.LOOP, comp.LoopStatement),
                new CompiletteAction(TokenType.WHILE, comp.WhileStatement),
                new CompiletteAction(TokenType.FOR, comp.ForStatement),
                new CompiletteAction(TokenType.OPEN_BRACE, comp.BlockStatement),
                new CompiletteAction(TokenType.THROW, comp.ThrowStatement));

            comp.SetPrattRules(
                (TokenType.MINUS, new ParseRule(comp.Unary, comp.Binary, Precedence.Term)),
                (TokenType.PLUS, new ParseRule(null, comp.Binary, Precedence.Term)),
                (TokenType.SLASH, new ParseRule(null, comp.Binary, Precedence.Factor)),
                (TokenType.STAR, new ParseRule(null, comp.Binary, Precedence.Factor)),
                (TokenType.PERCENT, new ParseRule(null, comp.Binary, Precedence.Factor)),
                (TokenType.BANG, new ParseRule(comp.Unary, null, Precedence.None)),
                (TokenType.INT, new ParseRule(comp.Literal, null, Precedence.None)),
                (TokenType.FLOAT, new ParseRule(comp.Literal, null, Precedence.None)),
                (TokenType.TRUE, new ParseRule(comp.Literal, null, Precedence.None)),
                (TokenType.FALSE, new ParseRule(comp.Literal, null, Precedence.None)),
                (TokenType.NULL, new ParseRule(comp.Literal, null, Precedence.None)),
                (TokenType.BANG_EQUAL, new ParseRule(null, comp.Binary, Precedence.Equality)),
                (TokenType.EQUALITY, new ParseRule(null, comp.Binary, Precedence.Equality)),
                (TokenType.LESS, new ParseRule(null, comp.Binary, Precedence.Comparison)),
                (TokenType.LESS_EQUAL, new ParseRule(null, comp.Binary, Precedence.Comparison)),
                (TokenType.GREATER, new ParseRule(null, comp.Binary, Precedence.Comparison)),
                (TokenType.GREATER_EQUAL, new ParseRule(null, comp.Binary, Precedence.Comparison)),
                (TokenType.STRING, new ParseRule(comp.Literal, null, Precedence.None)),
                (TokenType.IDENTIFIER, new ParseRule(comp.Variable, null, Precedence.None)),
                (TokenType.AND, new ParseRule(null, comp.And, Precedence.And)),
                (TokenType.OR, new ParseRule(null, comp.Or, Precedence.Or)),
                (TokenType.OPEN_PAREN, new ParseRule(comp.Grouping, comp.Call, Precedence.Call)));
        }

        public static void Dot(this CompilerBase comp, bool canAssign)
        {
            comp.Consume(TokenType.IDENTIFIER, "Expect property name after '.'.");
            byte name = comp.AddStringConstant();

            if (canAssign && comp.Match(TokenType.ASSIGN))
            {
                comp.Expression();
                comp.EmitOpAndBytes(OpCode.SET_PROPERTY, name);
            }
            else if (comp.Match(TokenType.OPEN_PAREN))
            {
                var argCount = comp.ArgumentList();
                comp.EmitOpAndBytes(OpCode.INVOKE, name);
                comp.EmitBytes(argCount);
            }
            else
            {
                comp.EmitOpAndBytes(OpCode.GET_PROPERTY, name);
            }
        }
    }
}
