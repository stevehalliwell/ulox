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

        public static void SetPrattRules(this CompilerBase comp, params (TokenType tt, IParseRule rule)[] rules)
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
                (TokenType.MINUS, new ActionParseRule(Unary, Binary, Precedence.Term)),
                (TokenType.PLUS, new ActionParseRule(null, Binary, Precedence.Term)),
                (TokenType.SLASH, new ActionParseRule(null, Binary, Precedence.Factor)),
                (TokenType.STAR, new ActionParseRule(null, Binary, Precedence.Factor)),
                (TokenType.PERCENT, new ActionParseRule(null, Binary, Precedence.Factor)),
                (TokenType.BANG, new ActionParseRule(Unary, null, Precedence.None)),
                (TokenType.INT, new ActionParseRule(Literal, null, Precedence.None)),
                (TokenType.FLOAT, new ActionParseRule(Literal, null, Precedence.None)),
                (TokenType.TRUE, new ActionParseRule(Literal, null, Precedence.None)),
                (TokenType.FALSE, new ActionParseRule(Literal, null, Precedence.None)),
                (TokenType.NULL, new ActionParseRule(Literal, null, Precedence.None)),
                (TokenType.BANG_EQUAL, new ActionParseRule(null, Binary, Precedence.Equality)),
                (TokenType.EQUALITY, new ActionParseRule(null, Binary, Precedence.Equality)),
                (TokenType.LESS, new ActionParseRule(null, Binary, Precedence.Comparison)),
                (TokenType.LESS_EQUAL, new ActionParseRule(null, Binary, Precedence.Comparison)),
                (TokenType.GREATER, new ActionParseRule(null, Binary, Precedence.Comparison)),
                (TokenType.GREATER_EQUAL, new ActionParseRule(null, Binary, Precedence.Comparison)),
                (TokenType.STRING, new ActionParseRule(Literal, null, Precedence.None)),
                (TokenType.IDENTIFIER, new ActionParseRule(Variable, null, Precedence.None)),
                (TokenType.AND, new ActionParseRule(null, And, Precedence.And)),
                (TokenType.OR, new ActionParseRule(null, Or, Precedence.Or)),
                (TokenType.OPEN_PAREN, new ActionParseRule(Grouping, Call, Precedence.Call)),
                (TokenType.CONTEXT_NAME_FUNC, new ActionParseRule(FName, null, Precedence.None)),
                (TokenType.DOT, new ActionParseRule(null, Dot, Precedence.Call))
                              );
        }

        public static void Dot(CompilerBase compiler, bool canAssign)
        {
            compiler.Consume(TokenType.IDENTIFIER, "Expect property name after '.'.");
            byte name = compiler.AddStringConstant();

            if (canAssign && compiler.Match(TokenType.ASSIGN))
            {
                compiler.Expression();
                compiler.EmitOpAndBytes(OpCode.SET_PROPERTY, name);
            }
            else if (compiler.Match(TokenType.OPEN_PAREN))
            {
                var argCount = compiler.ArgumentList();
                compiler.EmitOpAndBytes(OpCode.INVOKE, name);
                compiler.EmitBytes(argCount);
            }
            else
            {
                compiler.EmitOpAndBytes(OpCode.GET_PROPERTY, name);
            }
        }

        public static void FName(CompilerBase compiler, bool canAssign)
        {
            var fname = compiler.CurrentChunk.Name;
            compiler.CurrentChunk.AddConstantAndWriteInstruction(Value.New(fname), compiler.PreviousToken.Line);
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

            compiler.ConsumeEndStatement();
            compiler.EmitOpCode(OpCode.THROW);
        }

        public static void ContinueStatement(CompilerBase compiler)
        {
            var comp = compiler.CurrentCompilerState;
            if (comp.loopStates.Count == 0)
                throw new CompilerException("Cannot continue when not inside a loop.");

            compiler.EmitLoop(comp.loopStates.Peek().loopContinuePoint);

            compiler.ConsumeEndStatement();
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

            compiler.ConsumeEndStatement();

            comp.loopStates.Peek().loopExitPatchLocations.Add(exitJump);
        }

        public static void YieldStatement(CompilerBase compiler)
        {
            compiler.EmitOpCode(OpCode.YIELD);

            compiler.ConsumeEndStatement();
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

        public static void Unary(CompilerBase compiler, bool canAssign)
        {
            var op = compiler.PreviousToken.TokenType;

            compiler.ParsePrecedence(Precedence.Unary);

            switch (op)
            {
            case TokenType.MINUS: compiler.EmitOpCode(OpCode.NEGATE); break;
            case TokenType.BANG: compiler.EmitOpCode(OpCode.NOT); break;
            default:
                break;
            }
        }

        public static void Binary(CompilerBase compiler, bool canAssign)
        {
            TokenType operatorType = compiler.PreviousToken.TokenType;

            // Compile the right operand.
            var rule = compiler.PrattParser.GetRule(operatorType);
            compiler.ParsePrecedence((Precedence)(rule.Precedence + 1));

            switch (operatorType)
            {
            case TokenType.PLUS: compiler.EmitOpCode(OpCode.ADD); break;
            case TokenType.MINUS: compiler.EmitOpCode(OpCode.SUBTRACT); break;
            case TokenType.STAR: compiler.EmitOpCode(OpCode.MULTIPLY); break;
            case TokenType.SLASH: compiler.EmitOpCode(OpCode.DIVIDE); break;
            case TokenType.PERCENT: compiler.EmitOpCode(OpCode.MODULUS); break;
            case TokenType.EQUALITY: compiler.EmitOpCode(OpCode.EQUAL); break;
            case TokenType.GREATER: compiler.EmitOpCode(OpCode.GREATER); break;
            case TokenType.LESS: compiler.EmitOpCode(OpCode.LESS); break;
            case TokenType.BANG_EQUAL: compiler.EmitOpCodes(OpCode.EQUAL, OpCode.NOT); break;
            case TokenType.GREATER_EQUAL: compiler.EmitOpCodes(OpCode.LESS, OpCode.NOT); break;
            case TokenType.LESS_EQUAL: compiler.EmitOpCodes(OpCode.GREATER, OpCode.NOT); break;

            default:
                break;
            }
        }

        public static void Literal(CompilerBase compiler, bool canAssign)
        {
            switch (compiler.PreviousToken.TokenType)
            {
            case TokenType.TRUE: compiler.EmitOpAndBytes(OpCode.PUSH_BOOL, 1); break;
            case TokenType.FALSE: compiler.EmitOpAndBytes(OpCode.PUSH_BOOL, 0); break;
            case TokenType.NULL: compiler.EmitOpCode(OpCode.NULL); break;
            case TokenType.INT:
            case TokenType.FLOAT:
                {
                    var number = (double)compiler.PreviousToken.Literal;

                    var isInt = number == System.Math.Truncate(number);

                    if (isInt && number < 255 && number >= 0)
                        compiler.EmitOpAndBytes(OpCode.PUSH_BYTE, (byte)number);
                    else
                        compiler.CurrentChunk.AddConstantAndWriteInstruction(Value.New(number), compiler.PreviousToken.Line);
                }
                break;

            case TokenType.STRING:
                {
                    var str = (string)compiler.PreviousToken.Literal;
                    compiler.CurrentChunk.AddConstantAndWriteInstruction(Value.New(str), compiler.PreviousToken.Line);
                }
                break;
            }
        }

        public static void Variable(CompilerBase compiler, bool canAssign)
        {
            var name = (string)compiler.PreviousToken.Literal;
            compiler.NamedVariable(name, canAssign);
        }

        public static void And(CompilerBase compiler, bool canAssign)
        {
            int endJump = compiler.EmitJump(OpCode.JUMP_IF_FALSE);

            compiler.EmitOpCode(OpCode.POP);
            compiler.ParsePrecedence(Precedence.And);

            compiler.PatchJump(endJump);
        }

        public static void Or(CompilerBase compiler, bool canAssign)
        {
            int elseJump = compiler.EmitJump(OpCode.JUMP_IF_FALSE);
            int endJump = compiler.EmitJump(OpCode.JUMP);

            compiler.PatchJump(elseJump);
            compiler.EmitOpCode(OpCode.POP);

            compiler.ParsePrecedence(Precedence.Or);

            compiler.PatchJump(endJump);
        }

        public static void Grouping(CompilerBase compiler, bool canAssign)
        {
            compiler.ExpressionList(TokenType.CLOSE_PAREN, "Expect ')' after expression.");
        }

        public static void Call(CompilerBase compiler, bool canAssign)
        {
            var argCount = compiler.ArgumentList();
            compiler.EmitOpAndBytes(OpCode.CALL, argCount);
        }
    }
}
