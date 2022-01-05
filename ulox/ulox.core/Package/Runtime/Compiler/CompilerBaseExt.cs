namespace ULox
{
    public static class CompilerBaseExt
    {
        public static void AddDeclarationCompilette(this CompilerBase comp, (TokenType match, System.Action<CompilerBase> action) processAction)
        {
            comp.AddDeclarationCompilette(new CompiletteAction(processAction.match, processAction.action));
        }

        public static void AddDeclarationCompilette(this CompilerBase comp, params ICompilette[] compilettes)
        {
            foreach (var item in compilettes)
            {
                comp.AddDeclarationCompilette(item);
            }
        }

        public static void AddDeclarationCompilette(this CompilerBase comp, params (TokenType match, System.Action<CompilerBase> action)[] processActions)
        {
            foreach (var item in processActions)
            {
                comp.AddDeclarationCompilette(item);
            }
        }

        public static void AddStatementCompilette(this CompilerBase comp, (TokenType match, System.Action<CompilerBase> action) processAction)
        {
            comp.AddStatementCompilette(new CompiletteAction(processAction.match, processAction.action));
        }

        public static void AddStatementCompilette(this CompilerBase comp, params (TokenType match, System.Action<CompilerBase> action)[] processActions)
        {
            foreach (var item in processActions)
            {
                comp.AddStatementCompilette(item);
            }
        }

        public static void AddStatementCompilette(this CompilerBase comp, params ICompilette[] compilettes)
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
            comp.AddDeclarationCompilette(
                new VarDeclarationCompilette()
                                          );
            comp.AddDeclarationCompilette(
                (TokenType.FUNCTION, FunctionDeclaration)
                                          );
            comp.AddStatementCompilette(
                new ReturnStatementCompilette(),
                new LoopStatementCompilette(),
                new WhileStatementCompilette(),
                new ForStatementCompilette()
                );
            comp.AddStatementCompilette(
                (TokenType.IF, IfStatement),
                (TokenType.YIELD, YieldStatement),
                (TokenType.BREAK, BreakStatement),
                (TokenType.CONTINUE, ContinueStatement),
                (TokenType.OPEN_BRACE, BlockStatement),
                (TokenType.THROW, ThrowStatement),
                (TokenType.END_STATEMENT, NoOpStatement)
                                        );

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
                (TokenType.OPEN_PAREN, new ParseRule(comp.Grouping, comp.Call, Precedence.Call)),
                (TokenType.CONTEXT_NAME_FUNC, new ParseRule(comp.FName, null, Precedence.None))
                              );
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

        public static void FName(this CompilerBase comp, bool canAssign)
        {
            var fname = comp.CurrentChunk.Name;
            comp.CurrentChunk.AddConstantAndWriteInstruction(Value.New(fname), comp.PreviousToken.Line);
        }

        public static void ThrowStatement(CompilerBase compiler)
        {
            if (!compiler.Check(TokenType.END_STATEMENT))
            {
                compiler.Expression();
            }
            else
            {
                compiler.EmitOpCode(OpCode.NULL);
            }

            compiler.Consume(TokenType.END_STATEMENT, "Expect ; after throw statement.");
            compiler.EmitOpCode(OpCode.THROW);
        }

        public static void ContinueStatement(CompilerBase compiler)
        {
            var comp = compiler.CurrentCompilerState;
            if (comp.loopStates.Count == 0)
                throw new CompilerException("Cannot continue when not inside a loop.");

            compiler.EmitLoop(comp.loopStates.Peek().loopContinuePoint);

            compiler.Consume(TokenType.END_STATEMENT, "Expect ';' after continue.");
        }

        public static void IfStatement(CompilerBase compiler)
        {
            compiler.Consume(TokenType.OPEN_PAREN, "Expect '(' after if.");
            compiler.Expression();
            compiler.Consume(TokenType.CLOSE_PAREN, "Expect ')' after if.");

            int thenjump = compiler.EmitJump(OpCode.JUMP_IF_FALSE);
            compiler.EmitOpCode(OpCode.POP);

            compiler.Statement();

            int elseJump = compiler.EmitJump(OpCode.JUMP);

            compiler.PatchJump(thenjump);
            compiler.EmitOpCode(OpCode.POP);

            if (compiler.Match(TokenType.ELSE)) compiler.Statement();

            compiler.PatchJump(elseJump);
        }

        public static void BreakStatement(CompilerBase compiler)
        {
            var comp = compiler.CurrentCompilerState;
            if (comp.loopStates.Count == 0)
                throw new CompilerException("Cannot break when not inside a loop.");

            compiler.EmitOpCode(OpCode.NULL);
            int exitJump = compiler.EmitJump(OpCode.JUMP);

            //todo repeat
            compiler.Consume(TokenType.END_STATEMENT, "Expect ';' after break.");

            comp.loopStates.Peek().loopExitPatchLocations.Add(exitJump);
        }

        public static void YieldStatement(CompilerBase compiler)
        {
            compiler.EmitOpCode(OpCode.YIELD);

            //todo repeat
            compiler.Consume(TokenType.END_STATEMENT, "Expect ';' after yield.");
        }

        public static void BlockStatement(CompilerBase compiler)
            => compiler.BlockStatement();


        public static void FunctionDeclaration(CompilerBase compiler)
        {
            var global = compiler.ParseVariable("Expect function name.");
            compiler.CurrentCompilerState.MarkInitialised();

            compiler.Function(compiler.CurrentChunk.ReadConstant(global).val.asString.String, FunctionType.Function);
            compiler.DefineVariable(global);
        }

        public static void NoOpStatement(CompilerBase compiler)
        {
        }
    }
}
