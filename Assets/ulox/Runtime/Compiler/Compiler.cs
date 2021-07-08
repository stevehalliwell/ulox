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

        #region Declarations

        private void TestDeclaration()
        {
            //grab name
            var testClassName = (string)CurrentToken.Literal;
            CurrentChunk.AddConstant(Value.New(testClassName));

            //find the class by name, need to note this instruction so we can patch the argID as it doesn't exist yet
            //var argID = ResolveLocal(compilerStates.Peek(), testClassName);
            //create instance
            //

            //parse as class, class needs to add calls for all testcases it finds to the testFuncChunk
            ClassDeclaration();

            EmitOpCode(OpCode.NULL);
            EmitOpAndByte(OpCode.ASSIGN_GLOBAL_UNCACHED, CurrentChunk.AddConstant(Value.New(testClassName)));
        }

        private void ClassDeclaration()
        {
            Consume(TokenType.IDENTIFIER, "Expect class name.");
            var className = (string)PreviousToken.Literal;
            var compState = CurrentCompilerState;
            compState.classCompilerStates.Push(new ClassCompilerState(className));

            byte nameConstant = AddStringConstant();
            DeclareVariable();

            EmitOpAndByte(OpCode.CLASS, nameConstant);
            DefineVariable(nameConstant);

            bool hasSuper = false;

            if (Match(TokenType.LESS))
            {
                Consume(TokenType.IDENTIFIER, "Expect superclass name.");
                Variable(false);
                if (className == (string)PreviousToken.Literal)
                    throw new CompilerException("A class cannot inhert from itself.");

                BeginScope();
                AddLocal(compState, "super");
                DefineVariable(0);

                NamedVariable(className, false);
                EmitOpCode(OpCode.INHERIT);
                hasSuper = true;
            }

            NamedVariable(className, false);
            Consume(TokenType.OPEN_BRACE, "Expect '{' before class body.");
            while (!Check(TokenType.CLOSE_BRACE) && !Check(TokenType.EOF))
            {
                if (Match(TokenType.STATIC))
                {
                    if (Match(TokenType.VAR))
                    {
                        Property(true);
                    }
                    else
                    {
                        Method(true);
                    }
                }
                else if (Match(TokenType.VAR))
                    Property(false);
                else if (Match(TokenType.TESTCASE))
                    TestCase();
                else
                    Method(false);
            }

            //emit return //if we are the last link in the chain this ends our call
            var classCompState = CurrentCompilerState.classCompilerStates.Peek();

            if (classCompState.initFragStartLocation != -1)
            {
                EmitOpCode(OpCode.INIT_CHAIN_START);
                EmitUShort((ushort)classCompState.initFragStartLocation);
            }

            if (classCompState.testFragStartLocation != -1)
            {
                EmitOpCode(OpCode.TEST_CHAIN_START);
                EmitUShort((ushort)classCompState.testFragStartLocation);
            }

            //return stub used by init and test chains
            var classReturnEnd = EmitJump(OpCode.JUMP);

            if (classCompState.previousInitFragJumpLocation != -1)
                PatchJump(classCompState.previousInitFragJumpLocation);

            if (classCompState.previousTestFragJumpLocation != -1)
                PatchJump(classCompState.previousTestFragJumpLocation);

            //EmitOpCode(OpCode.NULL);
            EmitOpCode(OpCode.RETURN);

            PatchJump(classReturnEnd);

            Consume(TokenType.CLOSE_BRACE, "Expect '}' after class body.");
            EmitOpCode(OpCode.POP);

            if (hasSuper)
            {
                EndScope();
            }

            compState.classCompilerStates.Pop();
        }

        private void FunctionDeclaration()
        {
            var global = ParseVariable("Expect function name.");
            MarkInitialised();

            Function(CurrentChunk.ReadConstant(global).val.asString, FunctionType.Function);
            DefineVariable(global);
        }

        private void VarDeclaration()
        {
            do
            {
                var global = ParseVariable("Expect variable name");

                if (Match(TokenType.ASSIGN))
                    Expression();
                else
                    EmitOpCode(OpCode.NULL);

                DefineVariable(global);

            } while (Match(TokenType.COMMA));

            Consume(TokenType.END_STATEMENT, "Expect ; after variable declaration.");
        }

        #endregion Declarations

        private void Method(bool isStatic)
        {
            Consume(TokenType.IDENTIFIER, "Expect method name.");
            byte constant = AddStringConstant();

            var name = CurrentChunk.ReadConstant(constant).val.asString;
            var funcType = isStatic ? FunctionType.Function : FunctionType.Method;
            if (name == "init")
                funcType = FunctionType.Init;

            Function(name, funcType);
            EmitOpAndByte(OpCode.METHOD, constant);
        }

        private void Property(bool isStatic)
        {
            do
            {
                Consume(TokenType.IDENTIFIER, "Expect var name.");
                byte nameConstant = AddStringConstant();

                var compState = CurrentCompilerState;
                var classCompState = compState.classCompilerStates.Peek();

                int initFragmentJump = -1;
                if (!isStatic)
                {
                    //emit jump // to skip this during imperative
                    initFragmentJump = EmitJump(OpCode.JUMP);
                    //patch jump previous init fragment if it exists
                    if (classCompState.previousInitFragJumpLocation != -1)
                    {
                        PatchJump(classCompState.previousInitFragJumpLocation);
                    }
                    else
                    {
                        classCompState.initFragStartLocation = CurrentChunk.Instructions.Count;
                    }
                }


                EmitOpAndByte(OpCode.GET_LOCAL, (byte)(isStatic ? 1 : 0));//get class or inst this on the stack


                //if = consume it and then
                //eat 1 expression or a push null
                if (Match(TokenType.ASSIGN))
                {
                    Expression();
                }
                else
                {
                    EmitOpCode(OpCode.NULL);
                }

                //emit set prop
                EmitOpAndByte(OpCode.SET_PROPERTY_UNCACHED, nameConstant);
                EmitOpCode(OpCode.POP);
                if (!isStatic)
                {
                    //emit jump // to move to next prop init fragment, defaults to jump nowhere return
                    classCompState.previousInitFragJumpLocation = EmitJump(OpCode.JUMP);

                    //patch jump from skip imperative
                    PatchJump(initFragmentJump);
                }

            } while (Match(TokenType.COMMA));

            Consume(TokenType.END_STATEMENT, "Expect ; after property declaration.");
        }

        private void TestCase()
        {
            Consume(TokenType.IDENTIFIER, "Expect testcase name.");
            byte nameConstantID = AddStringConstant();

            var name = CurrentChunk.ReadConstant(nameConstantID).val.asString;

            var compState = CurrentCompilerState;
            var classCompState = compState.classCompilerStates.Peek();

            //emit jump // to skip this during imperative
            int testFragmentJump = EmitJump(OpCode.JUMP);
            //patch jump previous init fragment if it exists
            if (classCompState.previousTestFragJumpLocation != -1)
            {
                PatchJump(classCompState.previousTestFragJumpLocation);
            }
            else
            {
                classCompState.testFragStartLocation = compState.chunk.Instructions.Count;
            }

            Consume(TokenType.OPEN_BRACE, "Expect '{' before function body.");

            // The body.
            EmitOpAndByte(OpCode.TEST_START, nameConstantID);
            BlockStatement();
            EmitOpAndByte(OpCode.TEST_END, nameConstantID);

            classCompState.previousTestFragJumpLocation = EmitJump(OpCode.JUMP);

            //emit jump to step to next and save it
            PatchJump(testFragmentJump);
        }

        #region Statements
        private void IfStatement()
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

        private void ReturnStatement()
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

        private void YieldStatement()
        {
            EmitOpCode(OpCode.YIELD);
            Consume(TokenType.END_STATEMENT, "Expect ';' after break.");
        }

        private void BreakStatement()
        {
            var comp = CurrentCompilerState;
            if (comp.loopStates.Count == 0)
                throw new CompilerException("Cannot break when not inside a loop.");

            int exitJump = EmitJump(OpCode.JUMP);

            Consume(TokenType.END_STATEMENT, "Expect ';' after break.");

            comp.loopStates.Peek().loopExitPatchLocations.Add(exitJump);
        }

        private void ContinueStatement()
        {
            var comp = CurrentCompilerState;
            if (comp.loopStates.Count == 0)
                throw new CompilerException("Cannot continue when not inside a loop.");

            EmitLoop(comp.loopStates.Peek().loopStart);

            Consume(TokenType.END_STATEMENT, "Expect ';' after break.");
        }

        private void LoopStatement()
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

        private void WhileStatement()
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

        private void ForStatement()
        {
            BeginScope();

            Consume(TokenType.OPEN_PAREN, "Expect '(' after 'for'.");
            if (Match(TokenType.END_STATEMENT))
            {
                // No initializer.
            }
            else if (Match(TokenType.VAR))
            {
                VarDeclaration();
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

        private void BlockStatement()
        {
            BeginScope();
            Block();
            EndScope();
        }

        private void ThrowStatement()
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

        private void Number(bool canAssign)
        {
            var number = (double)PreviousToken.Literal;

            var isInt = number == System.Math.Truncate(number);

            if (isInt && number < 255 && number >= 0)
                EmitOpAndByte(OpCode.PUSH_BYTE, (byte)number);
            else
                CurrentChunk.AddConstantAndWriteInstruction(Value.New(number), PreviousToken.Line);
        }

        private void Literal(bool canAssign)
        {
            switch (PreviousToken.TokenType)
            {
            case TokenType.TRUE: EmitOpAndByte(OpCode.PUSH_BOOL, 1); break;
            case TokenType.FALSE: EmitOpAndByte(OpCode.PUSH_BOOL, 0); break;
            case TokenType.NULL: EmitOpCode(OpCode.NULL); break;
            }
        }

        private void String(bool canAssign)
        {
            var str = (string)PreviousToken.Literal;
            CurrentChunk.AddConstantAndWriteInstruction(Value.New(str), PreviousToken.Line);
        }
        private void Variable(bool canAssign)
        {
            var name = (string)PreviousToken.Literal;
            NamedVariable(name, canAssign);
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
                EmitOpAndByte(OpCode.SET_PROPERTY_UNCACHED, name);
            }
            else if (Match(TokenType.OPEN_PAREN))
            {
                var argCount = ArgumentList();
                EmitOpAndByte(OpCode.INVOKE, name);
                EmitByte(argCount);
            }
            else
            {
                EmitOpAndByte(OpCode.GET_PROPERTY_UNCACHED, name);
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
