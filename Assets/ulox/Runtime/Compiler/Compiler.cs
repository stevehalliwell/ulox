using System.Collections.Generic;

namespace ULox
{
    public class Compiler : CompilerBase
    {
        protected override void NoDeclarationFound()
        {
            Statement();
        }

        protected override void NoStatementFound()
        {
            ExpressionStatement();
        }

        protected override void GenerateDeclarationLookup()
        {
            var classcomp = new ClassCompilette();
            var testdeclComp = new TestDeclarationCompilette(classcomp);
            classcomp.AddInnerDeclarationCompilette(new TestcaseCompillette(testdeclComp));
            var decl = new List<ICompilette>()
            {
                testdeclComp,
                classcomp,
                new CompiletteAction(TokenType.FUNCTION, FunctionDeclaration),
                new CompiletteAction(TokenType.VAR, VarDeclaration),
            };

            foreach (var item in decl)
                declarationCompilettes[item.Match] = item;
        }

        protected override void GenerateStatementLookup()
        {
            var statement = new List<CompiletteAction>()
            {
                new CompiletteAction(TokenType.IF, IfStatement),
                new CompiletteAction(TokenType.RETURN, ReturnStatement),
                new CompiletteAction(TokenType.YIELD, YieldStatement),
                new CompiletteAction(TokenType.BREAK, BreakStatement),
                new CompiletteAction(TokenType.CONTINUE, ContinueStatement),
                new CompiletteAction(TokenType.LOOP, LoopStatement),
                new CompiletteAction(TokenType.WHILE, WhileStatement),
                new CompiletteAction(TokenType.FOR, ForStatement),
                new CompiletteAction(TokenType.OPEN_BRACE, BlockStatement),
                new CompiletteAction(TokenType.THROW, ThrowStatement),
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
            rules[(int)TokenType.PERCENT] = new ParseRule(null, Binary, Precedence.Factor);
            rules[(int)TokenType.BANG] = new ParseRule(Unary, null, Precedence.None);
            rules[(int)TokenType.INT] = new ParseRule(Literal, null, Precedence.None);
            rules[(int)TokenType.FLOAT] = new ParseRule(Literal, null, Precedence.None);
            rules[(int)TokenType.TRUE] = new ParseRule(Literal, null, Precedence.None);
            rules[(int)TokenType.FALSE] = new ParseRule(Literal, null, Precedence.None);
            rules[(int)TokenType.NULL] = new ParseRule(Literal, null, Precedence.None);
            rules[(int)TokenType.BANG_EQUAL] = new ParseRule(null, Binary, Precedence.Equality);
            rules[(int)TokenType.EQUALITY] = new ParseRule(null, Binary, Precedence.Equality);
            rules[(int)TokenType.LESS] = new ParseRule(null, Binary, Precedence.Comparison);
            rules[(int)TokenType.LESS_EQUAL] = new ParseRule(null, Binary, Precedence.Comparison);
            rules[(int)TokenType.GREATER] = new ParseRule(null, Binary, Precedence.Comparison);
            rules[(int)TokenType.GREATER_EQUAL] = new ParseRule(null, Binary, Precedence.Comparison);
            rules[(int)TokenType.STRING] = new ParseRule(Literal, null, Precedence.None);
            rules[(int)TokenType.IDENTIFIER] = new ParseRule(Variable, null, Precedence.None);
            rules[(int)TokenType.AND] = new ParseRule(null, And, Precedence.And);
            rules[(int)TokenType.OR] = new ParseRule(null, Or, Precedence.Or);
            rules[(int)TokenType.OPEN_PAREN] = new ParseRule(Grouping, Call, Precedence.Call);
            rules[(int)TokenType.DOT] = new ParseRule(null, Dot, Precedence.Call);
            rules[(int)TokenType.THIS] = new ParseRule(This, null, Precedence.None);
            rules[(int)TokenType.SUPER] = new ParseRule(Super, null, Precedence.None);
        }

        #region Statements
        private void IfStatement(CompilerBase compiler)
        {
            Consume(TokenType.OPEN_PAREN, "Expect '(' after if.");
            Expression();
            Consume(TokenType.CLOSE_PAREN, "Expect ')' after if.");

            int thenjump = EmitJump(OpCode.JUMP_IF_FALSE);
            EmitOpCode(OpCode.POP);

            Statement();

            int elseJump = EmitJump(OpCode.JUMP);

            PatchJump(thenjump);
            EmitOpCode(OpCode.POP);

            if (Match(TokenType.ELSE)) Statement();

            PatchJump(elseJump);
        }

        private void ReturnStatement(CompilerBase compiler)
        {
            //if (compilerStates.Count <= 1)
            //    throw new CompilerException("Cannot return from a top-level statement.");

            if (Match(TokenType.END_STATEMENT))
            {
                EmitReturn();
            }
            else if (CurrentCompilerState.functionType == FunctionType.Init)
            {
                throw new CompilerException("Cannot return an expression from an 'init'.");
            }
            else
            {
                Expression();
                Consume(TokenType.END_STATEMENT, "Expect ';' after return value.");
                EmitOpCode(OpCode.RETURN);
            }
        }

        private void YieldStatement(CompilerBase compiler)
        {
            EmitOpCode(OpCode.YIELD);
            Consume(TokenType.END_STATEMENT, "Expect ';' after break.");
        }

        private void BreakStatement(CompilerBase compiler)
        {
            var comp = CurrentCompilerState;
            if (comp.loopStates.Count == 0)
                throw new CompilerException("Cannot break when not inside a loop.");

            int exitJump = EmitJump(OpCode.JUMP);

            Consume(TokenType.END_STATEMENT, "Expect ';' after break.");

            comp.loopStates.Peek().loopExitPatchLocations.Add(exitJump);
        }

        private void ContinueStatement(CompilerBase compiler)
        {
            var comp = CurrentCompilerState;
            if (comp.loopStates.Count == 0)
                throw new CompilerException("Cannot continue when not inside a loop.");

            EmitLoop(comp.loopStates.Peek().loopStart);

            Consume(TokenType.END_STATEMENT, "Expect ';' after break.");
        }

        private void LoopStatement(CompilerBase compiler)
        {
            var comp = CurrentCompilerState;
            var loopState = new CompilerState.LoopState();
            comp.loopStates.Push(loopState);
            loopState.loopStart = CurrentChunkInstructinCount;

            Statement();

            EmitLoop(loopState.loopStart);

            PatchLoopExits(loopState);

            EmitOpCode(OpCode.POP);
            comp.loopStates.Pop();
        }

        private void WhileStatement(CompilerBase compiler)
        {
            var comp = CurrentCompilerState;
            var loopState = new CompilerState.LoopState();
            comp.loopStates.Push(loopState);
            loopState.loopStart = CurrentChunkInstructinCount;

            Consume(TokenType.OPEN_PAREN, "Expect '(' after if.");
            Expression();
            Consume(TokenType.CLOSE_PAREN, "Expect ')' after if.");

            int exitJump = EmitJump(OpCode.JUMP_IF_FALSE);
            loopState.loopExitPatchLocations.Add(exitJump);

            EmitOpCode(OpCode.POP);
            Statement();

            EmitLoop(loopState.loopStart);

            PatchLoopExits(loopState);

            EmitOpCode(OpCode.POP);
            comp.loopStates.Pop();
        }

        private void ForStatement(CompilerBase compiler)
        {
            BeginScope();

            Consume(TokenType.OPEN_PAREN, "Expect '(' after 'for'.");
            if (Match(TokenType.END_STATEMENT))
            {
                // No initializer.
            }
            else if (Match(TokenType.VAR))
            {
                VarDeclaration(compiler);
            }
            else
            {
                ExpressionStatement();
            }

            var comp = CurrentCompilerState;
            int loopStart = CurrentChunkInstructinCount;
            var loopState = new CompilerState.LoopState();
            comp.loopStates.Push(loopState);
            loopState.loopStart = loopStart;

            int exitJump = -1;
            if (!Match(TokenType.END_STATEMENT))
            {
                Expression();
                Consume(TokenType.END_STATEMENT, "Expect ';' after loop condition.");

                // Jump out of the loop if the condition is false.
                exitJump = EmitJump(OpCode.JUMP_IF_FALSE);
                loopState.loopExitPatchLocations.Add(exitJump);
                EmitOpCode(OpCode.POP); // Condition.
            }

            if (!Match(TokenType.CLOSE_PAREN))
            {
                int bodyJump = EmitJump(OpCode.JUMP);

                int incrementStart = CurrentChunkInstructinCount;
                Expression();
                EmitOpCode(OpCode.POP);
                Consume(TokenType.CLOSE_PAREN, "Expect ')' after for clauses.");

                EmitLoop(loopStart);
                loopStart = incrementStart;
                PatchJump(bodyJump);
            }

            Statement();

            EmitLoop(loopStart);

            PatchLoopExits(loopState);

            if (exitJump != -1)
            {
                EmitOpCode(OpCode.POP); // Condition.
            }

            EndScope();
        }

        private void BlockStatement(CompilerBase compiler)
        {
            BeginScope();
            Block();
            EndScope();
        }

        private void ThrowStatement(CompilerBase compiler)
        {
            if (!Check(TokenType.END_STATEMENT))
            {
                Expression();
            }
            else
            {
                EmitOpCode(OpCode.NULL);
            }

            Consume(TokenType.END_STATEMENT, "Expect ; after throw statement.");
            EmitOpCode(OpCode.THROW);
        }
        #endregion Statements

        #region Expressions
        private void Unary(bool canAssign)
        {
            var op = PreviousToken.TokenType;

            ParsePrecedence(Precedence.Unary);

            switch (op)
            {
            case TokenType.MINUS: EmitOpCode(OpCode.NEGATE); break;
            case TokenType.BANG: EmitOpCode(OpCode.NOT); break;
            default:
                break;
            }
        }

        private void Binary(bool canAssign)
        {
            TokenType operatorType = PreviousToken.TokenType;

            // Compile the right operand.
            ParseRule rule = GetRule(operatorType);
            ParsePrecedence((Precedence)(rule.Precedence + 1));

            switch (operatorType)
            {
            case TokenType.PLUS: EmitOpCode(OpCode.ADD); break;
            case TokenType.MINUS: EmitOpCode(OpCode.SUBTRACT); break;
            case TokenType.STAR: EmitOpCode(OpCode.MULTIPLY); break;
            case TokenType.SLASH: EmitOpCode(OpCode.DIVIDE); break;
            case TokenType.PERCENT: EmitOpCode(OpCode.MODULUS); break;
            case TokenType.EQUALITY: EmitOpCode(OpCode.EQUAL); break;
            case TokenType.GREATER: EmitOpCode(OpCode.GREATER); break;
            case TokenType.LESS: EmitOpCode(OpCode.LESS); break;
            case TokenType.BANG_EQUAL: EmitOpCodePair(OpCode.EQUAL, OpCode.NOT); break;
            case TokenType.GREATER_EQUAL: EmitOpCodePair(OpCode.LESS, OpCode.NOT); break;
            case TokenType.LESS_EQUAL: EmitOpCodePair(OpCode.GREATER, OpCode.NOT); break;

            default:
                break;
            }

        }

        private void Literal(bool canAssign)
        {
            switch (PreviousToken.TokenType)
            {
            case TokenType.TRUE: EmitOpAndByte(OpCode.PUSH_BOOL, 1); break;
            case TokenType.FALSE: EmitOpAndByte(OpCode.PUSH_BOOL, 0); break;
            case TokenType.NULL: EmitOpCode(OpCode.NULL); break;
            case TokenType.INT:
            case TokenType.FLOAT:
                {

                    var number = (double)PreviousToken.Literal;

                    var isInt = number == System.Math.Truncate(number);

                    if (isInt && number < 255 && number >= 0)
                        EmitOpAndByte(OpCode.PUSH_BYTE, (byte)number);
                    else
                        CurrentChunk.AddConstantAndWriteInstruction(Value.New(number), PreviousToken.Line);
                }
                break;
            case TokenType.STRING:
                {
                    var str = (string)PreviousToken.Literal;
                    CurrentChunk.AddConstantAndWriteInstruction(Value.New(str), PreviousToken.Line);
                }
                break;
            }
        }

        private void Variable(bool canAssign)
        {
            NamedVariableFromPreviousToken(canAssign);
        }

        private void And(bool canAssign)
        {
            int endJump = EmitJump(OpCode.JUMP_IF_FALSE);

            EmitOpCode(OpCode.POP);
            ParsePrecedence(Precedence.And);

            PatchJump(endJump);
        }

        private void Or(bool canAssign)
        {
            int elseJump = EmitJump(OpCode.JUMP_IF_FALSE);
            int endJump = EmitJump(OpCode.JUMP);

            PatchJump(elseJump);
            EmitOpCode(OpCode.POP);

            ParsePrecedence(Precedence.Or);

            PatchJump(endJump);
        }

        private void Grouping(bool canAssign)
        {
            Expression();
            Consume(TokenType.CLOSE_PAREN, "Expect ')' after expression.");
        }

        private void Call(bool canAssign)
        {
            var argCount = ArgumentList();
            EmitOpAndByte(OpCode.CALL, argCount);
        }

        private void This(bool canAssign)
        {
            if (GetEnclosingClass() == null)
                throw new CompilerException("Cannot use this outside of a class declaration.");

            Variable(false);
        }

        private void Dot(bool canAssign)
        {
            Consume(TokenType.IDENTIFIER, "Expect property name after '.'.");
            byte name = AddStringConstant();

            if (canAssign && Match(TokenType.ASSIGN))
            {
                Expression();
                EmitOpAndByte(OpCode.SET_PROPERTY, name);
            }
            else if (Match(TokenType.OPEN_PAREN))
            {
                var argCount = ArgumentList();
                EmitOpAndByte(OpCode.INVOKE, name);
                EmitByte(argCount);
            }
            else
            {
                EmitOpAndByte(OpCode.GET_PROPERTY, name);
            }
        }

        private void Super(bool canAssign)
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
                EmitOpAndByte(OpCode.SUPER_INVOKE, nameID);
                EmitByte(argCount);
            }
            else
            {
                NamedVariable("super", false);
                EmitOpAndByte(OpCode.GET_SUPER, nameID);
            }
        }
        #endregion Expressions
    }
}
