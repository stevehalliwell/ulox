using System.Collections.Generic;

//TODO: Too big, refactor and make more configurable

namespace ULox
{

    //todo for self assign to work, we need to route it through the assign path and
    //  then dump into a grouping, once the grouping is back, we named var ourselves and then emit the math op
    public abstract class CompilerBase
    {
        private IndexableStack<CompilerState> compilerStates = new IndexableStack<CompilerState>();

        private Token currentToken, previousToken;
        private List<Token> tokens;
        private int tokenIndex;
        
        // if we have more than 1 compiler we want this to be static
        protected ParseRule[] rules;
        protected Dictionary<TokenType, Compilette> declarationCompilettes = new Dictionary<TokenType, Compilette>();
        protected Dictionary<TokenType, Compilette> statementCompilettes = new Dictionary<TokenType, Compilette>();

        private int CurrentChunkInstructinCount => CurrentChunk.Instructions.Count;
        private Chunk CurrentChunk => compilerStates.Peek().chunk;

        protected CompilerBase()
        {
            Reset();
        }

        protected abstract void GenerateDeclarationLookup();
        protected abstract void GenerateStatementLookup();
        protected abstract void GenerateParseRules();

        public void Reset()
        {
            GenerateDeclarationLookup();
            GenerateStatementLookup();
            GenerateParseRules();

            compilerStates = new IndexableStack<CompilerState>();
            currentToken = default;
            previousToken = default;
            tokenIndex = 0;
            tokens = null;

            PushCompilerState(string.Empty, FunctionType.Script);
        }

        public Chunk Compile(List<Token> inTokens)
        {
            tokens = inTokens;
            Advance();

            while (currentToken.TokenType != TokenType.EOF)
            {
                Declaration();
            }

            return EndCompile();
        }

        private void Declaration()
        {
            if(declarationCompilettes.TryGetValue(currentToken.TokenType, out var complette))
            {
                Advance();
                complette.Process(this);
                return;
            }

            Statement();
        }

        private void Statement()
        {
            if (statementCompilettes.TryGetValue(currentToken.TokenType, out var complette))
            {
                Advance();
                complette.Process(this);
                return;
            }

            ExpressionStatement();
        }
        private void ExpressionStatement()
        {
            Expression();
            Consume(TokenType.END_STATEMENT, "Expect ; after expression statement.");
            EmitOpCode(OpCode.POP);
        }

        private void Expression()
        {
            ParsePrecedence(Precedence.Assignment);
        }

        void ParsePrecedence(Precedence pre)
        {
            Advance();
            var rule = GetRule(previousToken.TokenType);
            if (rule.Prefix == null)
            {
                throw new CompilerException("Expected prefix handler, but got null.");
            }

            var canAssign = pre <= Precedence.Assignment;
            rule.Prefix(this,canAssign);

            while (pre <= GetRule(currentToken.TokenType).Precedence)
            {
                Advance();
                rule = GetRule(previousToken.TokenType);
                rule.Infix(this,canAssign);
            }

            if (canAssign && Match(TokenType.ASSIGN))
            {
                throw new CompilerException("Invalid assignment target.");
            }
        }

        private ParseRule GetRule(TokenType operatorType)
        {
            return rules[(int)operatorType];
        }

        protected static void TestDeclaration(CompilerBase compiler)
        {
            //grab name
            var testClassName = (string)compiler.currentToken.Literal;
            compiler.CurrentChunk.AddConstant(Value.New(testClassName));

            //find the class by name, need to note this instruction so we can patch the argID as it doesn't exist yet
            //var argID = ResolveLocal(compilerStates.Peek(), testClassName);
            //create instance
            //

            //parse as class, class needs to add calls for all testcases it finds to the testFuncChunk
            ClassDeclaration(compiler);

            compiler.EmitOpCode(OpCode.NULL);
            compiler.EmitOpAndByte(OpCode.ASSIGN_GLOBAL_UNCACHED, compiler.CurrentChunk.AddConstant(Value.New(testClassName)));
        }

        protected static void ClassDeclaration(CompilerBase compiler)
        {
            compiler.Consume(TokenType.IDENTIFIER, "Expect class name.");
            var className = (string)compiler.previousToken.Literal;
            var compState = compiler.compilerStates.Peek();
            compState.classCompilerStates.Push(new ClassCompilerState(className));

            byte nameConstant = compiler.AddStringConstant();
            compiler.DeclareVariable();

            compiler.EmitOpAndByte(OpCode.CLASS, nameConstant);
            compiler.DefineVariable(nameConstant);

            bool hasSuper = false;

            if (compiler.Match(TokenType.LESS))
            {
                compiler.Consume(TokenType.IDENTIFIER, "Expect superclass name.");
                Variable(compiler, false);
                if (className == (string)compiler.previousToken.Literal)
                    throw new CompilerException("A class cannot inhert from itself.");

                compiler.BeginScope();
                AddLocal(compState, "super");
                compiler.DefineVariable(0);

                compiler.NamedVariable(className, false);
                compiler.EmitOpCode(OpCode.INHERIT);
                hasSuper = true;
            }

            compiler.NamedVariable(className, false);
            compiler.Consume(TokenType.OPEN_BRACE, "Expect '{' before class body.");
            while (!compiler.Check(TokenType.CLOSE_BRACE) && !compiler.Check(TokenType.EOF))
            {
                if (compiler.Match(TokenType.STATIC))
                {
                    if (compiler.Match(TokenType.VAR))
                    {
                        compiler.Property(true);
                    }
                    else
                    {
                        compiler.Method(true);
                    }
                }
                else if (compiler.Match(TokenType.VAR))
                    compiler.Property(false);
                else if (compiler.Match(TokenType.TESTCASE))
                    compiler.TestCase();
                else
                    compiler.Method(false);
            }

            //emit return //if we are the last link in the chain this ends our call
            var classCompState = compiler.compilerStates.Peek().classCompilerStates.Peek();

            if (classCompState.initFragStartLocation != -1)
            {
                compiler.EmitOpCode(OpCode.INIT_CHAIN_START);
                compiler.EmitUShort((ushort)classCompState.initFragStartLocation);
            }

            if (classCompState.testFragStartLocation != -1)
            {
                compiler.EmitOpCode(OpCode.TEST_CHAIN_START);
                compiler.EmitUShort((ushort)classCompState.testFragStartLocation);
            }

            //return stub used by init and test chains
            var classReturnEnd = compiler.EmitJump(OpCode.JUMP);

            if (classCompState.previousInitFragJumpLocation != -1)
                compiler.PatchJump(classCompState.previousInitFragJumpLocation);

            if (classCompState.previousTestFragJumpLocation != -1)
                compiler.PatchJump(classCompState.previousTestFragJumpLocation);

            //EmitOpCode(OpCode.NULL);
            compiler.EmitOpCode(OpCode.RETURN);

            compiler.PatchJump(classReturnEnd);

            compiler.Consume(TokenType.CLOSE_BRACE, "Expect '}' after class body.");
            compiler.EmitOpCode(OpCode.POP);

            if (hasSuper)
            {
                compiler.EndScope();
            }

            compState.classCompilerStates.Pop();
        }

        protected static void FunctionDeclaration(CompilerBase compiler)
        {
            var global = compiler.ParseVariable("Expect function name.");
            compiler.MarkInitialised();

            compiler.Function(compiler.CurrentChunk.ReadConstant(global).val.asString, FunctionType.Function);
            compiler.DefineVariable(global);
        }

        protected static void VarDeclaration(CompilerBase compiler)
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

            compiler.Consume(TokenType.END_STATEMENT, "Expect ; after variable declaration.");
        }

        protected static void IfStatement(CompilerBase compiler)
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

        protected static void ReturnStatement(CompilerBase compiler)
        {
            //if (compilerStates.Count <= 1)
            //    throw new CompilerException("Cannot return from a top-level statement.");

            if (compiler.Match(TokenType.END_STATEMENT))
            {
                compiler.EmitReturn();
            }
            else if (compiler.compilerStates.Peek().functionType == FunctionType.Init)
            {
                throw new CompilerException("Cannot return an expression from an 'init'.");
            }
            else
            {
                compiler.Expression();
                compiler.Consume(TokenType.END_STATEMENT, "Expect ';' after return value.");
                compiler.EmitOpCode(OpCode.RETURN);
            }
        }

        protected static void YieldStatement(CompilerBase compiler)
        {
            compiler.EmitOpCode(OpCode.YIELD);
            compiler.Consume(TokenType.END_STATEMENT, "Expect ';' after break.");
        }

        protected static void BreakStatement(CompilerBase compiler)
        {
            var comp = compiler.compilerStates.Peek();
            if (comp.loopStates.Count == 0)
                throw new CompilerException("Cannot break when not inside a loop.");

            int exitJump = compiler.EmitJump(OpCode.JUMP);

            compiler.Consume(TokenType.END_STATEMENT, "Expect ';' after break.");

            comp.loopStates.Peek().loopExitPatchLocations.Add(exitJump);
        }

        protected static void ContinueStatement(CompilerBase compiler)
        {
            var comp = compiler.compilerStates.Peek();
            if (comp.loopStates.Count == 0)
                throw new CompilerException("Cannot continue when not inside a loop.");

            compiler.EmitLoop(comp.loopStates.Peek().loopStart);

            compiler.Consume(TokenType.END_STATEMENT, "Expect ';' after break.");
        }

        protected static void LoopStatement(CompilerBase compiler)
        {
            var comp = compiler.compilerStates.Peek();
            var loopState = new CompilerState.LoopState();
            comp.loopStates.Push(loopState);
            loopState.loopStart = compiler.CurrentChunkInstructinCount;

            compiler.Statement();

            compiler.EmitLoop(loopState.loopStart);

            compiler.PatchLoopExits(loopState);

            compiler.EmitOpCode(OpCode.POP);
            comp.loopStates.Pop();
        }

        protected static void WhileStatement(CompilerBase compiler)
        {
            var comp = compiler.compilerStates.Peek();
            var loopState = new CompilerState.LoopState();
            comp.loopStates.Push(loopState);
            loopState.loopStart = compiler.CurrentChunkInstructinCount;

            compiler.Consume(TokenType.OPEN_PAREN, "Expect '(' after if.");
            compiler.Expression();
            compiler.Consume(TokenType.CLOSE_PAREN, "Expect ')' after if.");

            int exitJump = compiler.EmitJump(OpCode.JUMP_IF_FALSE);
            loopState.loopExitPatchLocations.Add(exitJump);

            compiler.EmitOpCode(OpCode.POP);
            compiler.Statement();

            compiler.EmitLoop(loopState.loopStart);

            compiler.PatchLoopExits(loopState);

            compiler.EmitOpCode(OpCode.POP);
            comp.loopStates.Pop();
        }

        protected static void ForStatement(CompilerBase compiler)
        {
            compiler.BeginScope();

            compiler.Consume(TokenType.OPEN_PAREN, "Expect '(' after 'for'.");
            if (compiler.Match(TokenType.END_STATEMENT))
            {
                // No initializer.
            }
            else if (compiler.Match(TokenType.VAR))
            {
                VarDeclaration(compiler);
            }
            else
            {
                compiler.ExpressionStatement();
            }

            var comp = compiler.compilerStates.Peek();
            int loopStart = compiler.CurrentChunkInstructinCount;
            var loopState = new CompilerState.LoopState();
            comp.loopStates.Push(loopState);
            loopState.loopStart = loopStart;

            int exitJump = -1;
            if (!compiler.Match(TokenType.END_STATEMENT))
            {
                compiler.Expression();
                compiler.Consume(TokenType.END_STATEMENT, "Expect ';' after loop condition.");

                // Jump out of the loop if the condition is false.
                exitJump = compiler.EmitJump(OpCode.JUMP_IF_FALSE);
                loopState.loopExitPatchLocations.Add(exitJump);
                compiler.EmitOpCode(OpCode.POP); // Condition.
            }

            if (!compiler.Match(TokenType.CLOSE_PAREN))
            {
                int bodyJump = compiler.EmitJump(OpCode.JUMP);

                int incrementStart = compiler.CurrentChunkInstructinCount;
                compiler.Expression();
                compiler.EmitOpCode(OpCode.POP);
                compiler.Consume(TokenType.CLOSE_PAREN, "Expect ')' after for clauses.");

                compiler.EmitLoop(loopStart);
                loopStart = incrementStart;
                compiler.PatchJump(bodyJump);
            }

            compiler.Statement();

            compiler.EmitLoop(loopStart);

            compiler.PatchLoopExits(loopState);

            if (exitJump != -1)
            {
                compiler.EmitOpCode(OpCode.POP); // Condition.
            }

            compiler.EndScope();
        }

        protected static void BlockStatement(CompilerBase compiler)
        {
            compiler.BeginScope();
            compiler.Block();
            compiler.EndScope();
        }

        protected static void ThrowStatement(CompilerBase compiler)
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

        protected static void Unary(CompilerBase compiler, bool canAssign)
        {
            var op = compiler.previousToken.TokenType;

            compiler.ParsePrecedence(Precedence.Unary);

            switch (op)
            {
            case TokenType.MINUS: compiler.EmitOpCode(OpCode.NEGATE); break;
            case TokenType.BANG: compiler.EmitOpCode(OpCode.NOT); break;
            default:
                break;
            }
        }

        protected static void Binary(CompilerBase compiler, bool canAssign)
        {
            TokenType operatorType = compiler.previousToken.TokenType;

            // Compile the right operand.
            ParseRule rule = compiler.GetRule(operatorType);
            compiler.ParsePrecedence((Precedence)(rule.Precedence + 1));

            switch (operatorType)
            {
            case TokenType.PLUS: compiler.EmitOpCode(OpCode.ADD); break;
            case TokenType.MINUS: compiler.EmitOpCode(OpCode.SUBTRACT); break;
            case TokenType.STAR: compiler.EmitOpCode(OpCode.MULTIPLY); break;
            case TokenType.SLASH: compiler.EmitOpCode(OpCode.DIVIDE); break;
            case TokenType.EQUALITY: compiler.EmitOpCode(OpCode.EQUAL); break;
            case TokenType.GREATER: compiler.EmitOpCode(OpCode.GREATER); break;
            case TokenType.LESS: compiler.EmitOpCode(OpCode.LESS); break;
            case TokenType.BANG_EQUAL: compiler.EmitOpCodePair(OpCode.EQUAL, OpCode.NOT); break;
            case TokenType.GREATER_EQUAL: compiler.EmitOpCodePair(OpCode.LESS, OpCode.NOT); break;
            case TokenType.LESS_EQUAL: compiler.EmitOpCodePair(OpCode.GREATER, OpCode.NOT); break;

            default:
                break;
            }

        }

        protected static private void Number(CompilerBase compiler, bool canAssign)
        {
            var number = (double)compiler.previousToken.Literal;

            var isInt = number == System.Math.Truncate(number);

            if (isInt && number < 255 && number >= 0)
                compiler.EmitOpAndByte(OpCode.PUSH_BYTE, (byte)number);
            else
                compiler.CurrentChunk.AddConstantAndWriteInstruction(Value.New(number), compiler.previousToken.Line);
        }

        protected static void Literal(CompilerBase compiler, bool canAssign)
        {
            switch (compiler.previousToken.TokenType)
            {
            case TokenType.TRUE: compiler.EmitOpAndByte(OpCode.PUSH_BOOL, 1); break;
            case TokenType.FALSE: compiler.EmitOpAndByte(OpCode.PUSH_BOOL, 0); break;
            case TokenType.NULL: compiler.EmitOpCode(OpCode.NULL); break;
            }
        }

        protected static void String(CompilerBase compiler, bool canAssign)
        {
            var str = (string)compiler.previousToken.Literal;
            compiler.CurrentChunk.AddConstantAndWriteInstruction(Value.New(str), compiler.previousToken.Line);
        }

        protected static void Super(CompilerBase compiler, bool canAssign)
        {
            if (compiler.GetEnclosingClass() == null)
                throw new CompilerException("Cannot use super outside a class.");
            //todo cannot use outisde a class without a super

            compiler.Consume(TokenType.DOT, "Expect '.' after a super.");
            compiler.Consume(TokenType.IDENTIFIER, "Expect superclass method name.");
            var nameID = compiler.AddStringConstant();

            compiler.NamedVariable("this", false);
            if (compiler.Match(TokenType.OPEN_PAREN))
            {
                byte argCount = compiler.ArgumentList();
                compiler.NamedVariable("super", false);
                compiler.EmitOpAndByte(OpCode.SUPER_INVOKE, nameID);
                compiler.EmitByte(argCount);
            }
            else
            {
                compiler.NamedVariable("super", false);
                compiler.EmitOpAndByte(OpCode.GET_SUPER, nameID);
            }
        }

        protected static void This(CompilerBase compiler, bool canAssign)
        {
            if (compiler.GetEnclosingClass() == null)
                throw new CompilerException("Cannot use this outside of a class declaration.");

            Variable(compiler, false);
        }

        protected static void Dot(CompilerBase compiler, bool canAssign)
        {
            compiler.Consume(TokenType.IDENTIFIER, "Expect property name after '.'.");
            byte name = compiler.AddStringConstant();

            if (canAssign && compiler.Match(TokenType.ASSIGN))
            {
                compiler.Expression();
                compiler.EmitOpAndByte(OpCode.SET_PROPERTY_UNCACHED, name);
            }
            else if (compiler.Match(TokenType.OPEN_PAREN))
            {
                var argCount = compiler.ArgumentList();
                compiler.EmitOpAndByte(OpCode.INVOKE, name);
                compiler.EmitByte(argCount);
            }
            else
            {
                compiler.EmitOpAndByte(OpCode.GET_PROPERTY_UNCACHED, name);
            }
        }

        protected static void Call(CompilerBase compiler, bool canAssign)
        {
            var argCount = compiler.ArgumentList();
            compiler.EmitOpAndByte(OpCode.CALL, argCount);
        }

        protected static void And(CompilerBase compiler, bool canAssign)
        {
            int endJump = compiler.EmitJump(OpCode.JUMP_IF_FALSE);

            compiler.EmitOpCode(OpCode.POP);
            compiler.ParsePrecedence(Precedence.And);

            compiler.PatchJump(endJump);
        }

        protected static void Or(CompilerBase compiler, bool canAssign)
        {
            int elseJump = compiler.EmitJump(OpCode.JUMP_IF_FALSE);
            int endJump = compiler.EmitJump(OpCode.JUMP);

            compiler.PatchJump(elseJump);
            compiler.EmitOpCode(OpCode.POP);

            compiler.ParsePrecedence(Precedence.Or);

            compiler.PatchJump(endJump);
        }

        protected static void Grouping(CompilerBase compiler, bool canAssign)
        {
            compiler.Expression();
            compiler.Consume(TokenType.CLOSE_PAREN, "Expect ')' after expression.");
        }

        protected static void Variable(CompilerBase compiler, bool canAssign)
        {
            var name = (string)compiler.previousToken.Literal;
            compiler.NamedVariable(name, canAssign);
        }

        private void PushCompilerState(string name, FunctionType functionType)
        {
            compilerStates.Push(new CompilerState(compilerStates.Peek(), functionType)
            {
                chunk = new Chunk(name),
            });

            if (functionType == FunctionType.Method || functionType == FunctionType.Init)
                AddLocal(compilerStates.Peek(), "this",0);
            else
                AddLocal(compilerStates.Peek(), "", 0);
        }

        private byte ArgumentList()
        {
            byte argCount = 0;
            if (!Check(TokenType.CLOSE_PAREN))
            {
                do
                {
                    Expression();
                    if (argCount == 255)
                        throw new CompilerException("Can't have more than 255 arguments.");

                    argCount++;
                } while (Match(TokenType.COMMA));
            }

            Consume(TokenType.CLOSE_PAREN, "Expect ')' after arguments.");
            return argCount;
        }

        private string GetEnclosingClass()
        {
            for (int i = compilerStates.Count - 1; i >= 0; i--)
            {
                if (compilerStates[i].classCompilerStates.Count == 0)
                    continue;

                var cur = compilerStates[i].classCompilerStates.Peek();
                if (string.IsNullOrEmpty(cur.currentClassName))
                    continue;

                return cur.currentClassName;
            }

            return null;
        }

        private void TestCase()
        {
            Consume(TokenType.IDENTIFIER, "Expect testcase name.");
            byte nameConstantID = AddStringConstant();

            var name = CurrentChunk.ReadConstant(nameConstantID).val.asString;

            var compState = compilerStates.Peek();
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
            BeginScope();
            Block();
            EndScope();
            EmitOpAndByte(OpCode.TEST_END, nameConstantID);

            classCompState.previousTestFragJumpLocation = EmitJump(OpCode.JUMP);

            //emit jump to step to next and save it
            PatchJump(testFragmentJump);
        }

        private void Property(bool isStatic)
        {
            do
            {
                Consume(TokenType.IDENTIFIER, "Expect var name.");
                byte nameConstant = AddStringConstant();

                var compState = compilerStates.Peek();
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


                EmitOpAndByte(OpCode.GET_LOCAL, (byte)(isStatic ? 1: 0));//get class or inst this on the stack


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

        

        private void Function(string name, FunctionType functionType)
        {
            PushCompilerState(name, functionType);

            BeginScope();

            if (Match(TokenType.OPEN_PAREN))
            {
                // Compile the parameter list.
                //Consume(TokenType.OPEN_PAREN, "Expect '(' after function name.");
                if (!Check(TokenType.CLOSE_PAREN))
                {
                    do
                    {
                        CurrentChunk.Arity++;
                        if (CurrentChunk.Arity > 255)
                        {
                            throw new CompilerException("Can't have more than 255 parameters.");
                        }

                        var paramConstant = ParseVariable("Expect parameter name.");
                        DefineVariable(paramConstant);
                    } while (Match(TokenType.COMMA));
                }
                Consume(TokenType.CLOSE_PAREN, "Expect ')' after parameters.");
            }

            // The body.
            Consume(TokenType.OPEN_BRACE, "Expect '{' before function body.");
            Block();

            // Create the function object.
            var comp = compilerStates.Peek();   //we need this to mark upvalues
            var function = EndCompile();
            EmitOpAndByte(OpCode.CLOSURE, CurrentChunk.AddConstant(Value.New(function)));

            for (int i = 0; i < function.UpvalueCount; i++)
            {
                EmitByte(comp.upvalues[i].isLocal ? (byte)1 : (byte)0);
                EmitByte(comp.upvalues[i].index);
            }
        }

        private byte ParseVariable(string errMsg)
        {
            Consume(TokenType.IDENTIFIER, errMsg);

            DeclareVariable();
            if (compilerStates.Peek().scopeDepth > 0) return 0;
            return AddStringConstant();
        }

        private void DeclareVariable()
        {
            var comp = compilerStates.Peek();

            if (comp.scopeDepth == 0) return;

            var declName = comp.chunk.ReadConstant(AddStringConstant()).val.asString;

            for (int i = comp.localCount - 1; i >= 0; i--)
            {
                var local = comp.locals[i];
                if (local.Depth != -1 && local.Depth < comp.scopeDepth)
                    break;

                if (declName == local.Name)
                    throw new CompilerException($"Already a variable with name '{declName}' in this scope.");
            }

            AddLocal(comp, declName);
        }

        private static void AddLocal(CompilerState comp, string name, int depth = -1)
        {
            if (comp.localCount == byte.MaxValue)
                throw new CompilerException("Too many local variables.");

            comp.locals[comp.localCount++] = new Local(name, depth);
        }

        private void DefineVariable(byte global)
        {
            if (compilerStates.Peek().scopeDepth > 0)
            {
                MarkInitialised();
                return;
            }

            EmitOpAndByte(OpCode.DEFINE_GLOBAL, global);
        }

        private void MarkInitialised()
        {
            var comp = compilerStates.Peek();

            if (comp.scopeDepth == 0) return;
            comp.locals[comp.localCount - 1].Depth = comp.scopeDepth;
        }

        private void BeginScope()
        {
            compilerStates.Peek().scopeDepth++;
        }

        private void EndScope()
        {
            var comp = compilerStates.Peek();

            comp.scopeDepth--;

            while (comp.localCount > 0 &&
                comp.locals[comp.localCount - 1].Depth > comp.scopeDepth)
            {
                if(comp.locals[comp.localCount - 1].IsCaptured)
                    EmitOpCode(OpCode.CLOSE_UPVALUE);
                else
                    EmitOpCode(OpCode.POP);

                compilerStates.Peek().localCount--;
            }
        }

        private int ResolveUpvalue (CompilerState compilerState, string name)
        {
            if (compilerState.enclosing == null) return -1;

            int local = ResolveLocal(compilerState.enclosing, name);
            if (local != -1)
            {
                compilerState.enclosing.locals[local].IsCaptured = true;
                return AddUpvalue(compilerState, (byte)local, true);
            }

            int upvalue = ResolveUpvalue(compilerState.enclosing, name);
            if (upvalue != -1)
            {
                return AddUpvalue(compilerState, (byte)upvalue, false);
            }

            return -1;
        }


        private int AddUpvalue(CompilerState compilerState, byte index, bool isLocal)
        {
            int upvalueCount = compilerState.chunk.UpvalueCount;

            Upvalue upvalue = default;

            for (int i = 0; i < upvalueCount; i++)
            {
                upvalue = compilerState.upvalues[i];
                if (upvalue.index == index && upvalue.isLocal == isLocal)
                {
                    return i;
                }
            }

            if (upvalueCount == byte.MaxValue)
            {
                throw new CompilerException("Too many closure variables in function.");
            }

            compilerState.upvalues[upvalueCount] = new Upvalue(index,isLocal);
            return compilerState.chunk.UpvalueCount++;
        }

        private int ResolveLocal(CompilerState compilerState, string name)
        {
            for (int i = compilerState.localCount - 1; i >= 0; i--)
            {
                var local = compilerState.locals[i];
                if (name == local.Name)
                {
                    if (local.Depth == -1)
                        throw new CompilerException("Cannot referenece a variable in it's own initialiser.");
                    return i;
                }
            }

            return -1;
        }

        private void PatchLoopExits(CompilerState.LoopState loopState)
        {
            if (loopState.loopExitPatchLocations.Count == 0)
                throw new CompilerException("Loops must contain an termination.");

            for (int i = 0; i < loopState.loopExitPatchLocations.Count; i++)
            {
                PatchJump(loopState.loopExitPatchLocations[i]);
            }
        }

        private void PatchJump(int thenjump)
        {
            int jump = CurrentChunkInstructinCount - thenjump - 2;

            if (jump > ushort.MaxValue)
                throw new CompilerException($"Cannot jump '{jump}'. Max jump is '{ushort.MaxValue}'");

            WriteBytesAt(thenjump, (byte)((jump >> 8) & 0xff), (byte)(jump & 0xff));
        }

        private void Block()
        {
            while (!Check(TokenType.CLOSE_BRACE) && !Check(TokenType.EOF))
                Declaration();

            Consume(TokenType.CLOSE_BRACE, "Expect '}' after block.");
        }

        private void NamedVariable(string name, bool canAssign)
        {
            OpCode getOp = OpCode.FETCH_GLOBAL_UNCACHED, setOp = OpCode.ASSIGN_GLOBAL_UNCACHED;
            var argID = ResolveLocal(compilerStates.Peek(), name);
            if (argID != -1)
            {
                getOp = OpCode.GET_LOCAL;
                setOp = OpCode.SET_LOCAL;
            }
            else
            {
                argID = ResolveUpvalue(compilerStates.Peek(), name);
                if (argID != -1)
                {
                    getOp = OpCode.GET_UPVALUE;
                    setOp = OpCode.SET_UPVALUE;
                }
                else
                {
                    argID = CurrentChunk.AddConstant(Value.New(name));
                }
            }

            if (canAssign && MatchAny(TokenType.ASSIGN,
                                      TokenType.PLUS_EQUAL,
                                      TokenType.MINUS_EQUAL,
                                      TokenType.STAR_EQUAL,
                                      TokenType.SLASH_EQUAL))
            {
                var assignTokenType = previousToken.TokenType;

                Expression();

                // self assign ops have to be done here as they tail the previous ordered instructions
                switch (assignTokenType)
                {
                case TokenType.PLUS_EQUAL:
                    EmitOpAndByte(getOp, (byte)argID);
                    EmitOpCode(OpCode.ADD);
                    break;
                case TokenType.MINUS_EQUAL:
                    EmitOpAndByte(getOp, (byte)argID);
                    EmitOpCode(OpCode.SUBTRACT);
                    break;
                case TokenType.STAR_EQUAL:
                    EmitOpAndByte(getOp, (byte)argID);
                    EmitOpCode(OpCode.MULTIPLY);
                    break;
                case TokenType.SLASH_EQUAL:
                    EmitOpAndByte(getOp, (byte)argID);
                    EmitOpCode(OpCode.DIVIDE);
                    break;
                case TokenType.ASSIGN:
                    break;
                }

                EmitOpAndByte(setOp, (byte)argID);
            }
            else
            {
                EmitOpAndByte(getOp, (byte)argID);
            }
        }

        private byte AddStringConstant()
        {
            return CurrentChunk.AddConstant(Value.New((string)previousToken.Literal));
        }

        private Chunk EndCompile()
        {
            EmitReturn();
            return compilerStates.Pop().chunk;
        }

        private void EmitReturn()
        {
            if (compilerStates.Peek().functionType == FunctionType.Init)
                EmitOpAndByte(OpCode.GET_LOCAL, 0);
            else
                EmitOpCode(OpCode.NULL);
            
            EmitOpCode(OpCode.RETURN);
        }
        
        void Consume(TokenType tokenType, string msg)
        {
            if (currentToken.TokenType == tokenType)
                Advance();
            else
                throw new CompilerException(msg + $" at {previousToken.Line}:{previousToken.Character} '{previousToken.Literal}'");
        }

        bool Check(TokenType type)
        {
            return currentToken.TokenType == type;
        }

        bool Match(TokenType type)
        {
            if (!Check(type))
                return false;
            Advance();
            return true;
        }

        bool MatchAny(params TokenType[] type)
        {
            for (int i = 0; i < type.Length; i++)
            {
                if (!Check(type[i])) continue;

                Advance();
                return true;
            }
            return false;
        }

        void EmitOpCode(OpCode op)
        {
            CurrentChunk.WriteSimple(op, previousToken.Line);
        }

        void EmitOpCodePair(OpCode op1, OpCode op2)
        {
            CurrentChunk.WriteSimple(op1, previousToken.Line);
            CurrentChunk.WriteSimple(op2, previousToken.Line);
        }

        void EmitOpAndByte(OpCode op, byte b)
        {
            CurrentChunk.WriteSimple(op, previousToken.Line);
            CurrentChunk.WriteByte(b, previousToken.Line);
        }

        void EmitBytes(params byte[] b)
        {
            for (int i = 0; i < b.Length; i++)
            {
                CurrentChunk.WriteByte(b[i], previousToken.Line);
            }
        }

        void EmitByte(byte b)
        {
            CurrentChunk.WriteByte(b, previousToken.Line);
        }

        void EmitUShort(ushort us)
        {
            EmitBytes((byte)((us >> 8) & 0xff), (byte)(us & 0xff));
        }

        void WriteBytesAt(int at, params byte[] b)
        {
            for (int i = 0; i < b.Length; i++)
            {
                CurrentChunk.Instructions[at+i] = b[i];
            }
        }

        private int EmitJump(OpCode op)
        {
            EmitBytes((byte)op, 0xff, 0xff);
            return CurrentChunk.Instructions.Count - 2;
        }

        private void EmitLoop(int loopStart)
        {
            EmitOpCode(OpCode.LOOP);
            int offset = CurrentChunk.Instructions.Count - loopStart + 2;

            if (offset > ushort.MaxValue)
                throw new CompilerException($"Cannot loop '{offset}'. Max loop is '{ushort.MaxValue}'");

            EmitBytes((byte)((offset >> 8) & 0xff),(byte)(offset & 0xff));
        }

        private void Advance()
        {
            previousToken = currentToken;
            currentToken = tokens[tokenIndex];
            tokenIndex++;
        }

    }
}
