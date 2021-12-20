using System.Collections.Generic;

namespace ULox
{
    //todo for self assign to work, we need to route it through the assign path and
    //  then dump into a grouping, once the grouping is back, we named var ourselves and then emit the math op
    public abstract class CompilerBase
    {
        private IndexableStack<CompilerState> compilerStates = new IndexableStack<CompilerState>();

        public Token CurrentToken { get; private set; }
        public Token PreviousToken { get; private set; }
        private List<Token> tokens;
        private int tokenIndex;

        protected ParseRule[] rules;
        protected Dictionary<TokenType, ICompilette> declarationCompilettes = new Dictionary<TokenType, ICompilette>();
        protected Dictionary<TokenType, ICompilette> statementCompilettes = new Dictionary<TokenType, ICompilette>();

        public int CurrentChunkInstructinCount => CurrentChunk.Instructions.Count;
        public Chunk CurrentChunk => CurrentCompilerState.chunk;
        public CompilerState CurrentCompilerState => compilerStates.Peek();

        protected CompilerBase()
        {
            rules = new ParseRule[System.Enum.GetNames(typeof(TokenType)).Length];

            for (int i = 0; i < rules.Length; i++)
            {
                rules[i] = new ParseRule(null, null, Precedence.None);
            }

            Reset();
        }

        protected virtual void NoDeclarationFound()
        {
            Statement();
        }

        protected virtual void NoStatementFound()
        {
            ExpressionStatement();
        }

        public void AddDeclarationCompilette(ICompilette compilette)
            => declarationCompilettes[compilette.Match] = compilette;

        public void AddStatementCompilette(ICompilette compilette)
            => statementCompilettes[compilette.Match] = compilette;

        public void SetPrattRule(TokenType tt, ParseRule rule)
            => rules[(int)tt] = rule;

        public void Reset()
        {
            compilerStates = new IndexableStack<CompilerState>();
            CurrentToken = default;
            PreviousToken = default;
            tokenIndex = 0;
            tokens = null;

            PushCompilerState(string.Empty, FunctionType.Script);
        }

        public Chunk Compile(List<Token> inTokens)
        {
            tokens = inTokens;
            Advance();

            while (CurrentToken.TokenType != TokenType.EOF)
            {
                Declaration();
            }

            return EndCompile();
        }

        protected void Declaration()
        {
            if (declarationCompilettes.TryGetValue(CurrentToken.TokenType, out var complette))
            {
                Advance();
                complette.Process(this);
                return;
            }

            NoDeclarationFound();
        }

        protected void Statement()
        {
            if (statementCompilettes.TryGetValue(CurrentToken.TokenType, out var complette))
            {
                Advance();
                complette.Process(this);
                return;
            }

            NoStatementFound();
        }

        protected void ExpressionStatement()
        {
            Expression();
            Consume(TokenType.END_STATEMENT, "Expect ; after expression statement.");
            EmitOpCode(OpCode.POP);
        }

        public void Expression()
        {
            ParsePrecedence(Precedence.Assignment);
        }

        protected void ParsePrecedence(Precedence pre)
        {
            Advance();
            var rule = GetRule(PreviousToken.TokenType);
            if (rule.Prefix == null)
            {
                throw new CompilerException("Expected prefix handler, but got null.");
            }

            var canAssign = pre <= Precedence.Assignment;
            rule.Prefix(canAssign);

            while (pre <= GetRule(CurrentToken.TokenType).Precedence)
            {
                Advance();
                rule = GetRule(PreviousToken.TokenType);
                rule.Infix(canAssign);
            }

            if (canAssign && Match(TokenType.ASSIGN))
            {
                throw new CompilerException("Invalid assignment target.");
            }
        }

        protected ParseRule GetRule(TokenType operatorType)
        {
            return rules[(int)operatorType];
        }

        public void PushCompilerState(string name, FunctionType functionType)
        {
            compilerStates.Push(new CompilerState(compilerStates.Peek(), functionType)
            {
                chunk = new Chunk(name),
            });

            if (functionType == FunctionType.Method || functionType == FunctionType.Init)
                AddLocal(compilerStates.Peek(), "this", 0);
            else
                AddLocal(compilerStates.Peek(), "", 0);
        }

        public void AddLocal(CompilerState comp, string name, int depth = -1)
        {
            if (comp.localCount == byte.MaxValue)
                throw new CompilerException("Too many local variables.");

            comp.locals[comp.localCount++] = new Local(name, depth);
        }

        private int ResolveUpvalue(CompilerState compilerState, string name)
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

            compilerState.upvalues[upvalueCount] = new Upvalue(index, isLocal);
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
                        throw new CompilerException($"{name}. Cannot referenece a variable in it's own initialiser.");  //todo all of these throws need to report names and locations
                    return i;
                }
            }

            return -1;
        }

        public void NamedVariableFromPreviousToken(bool canAssign)
        {
            var name = (string)PreviousToken.Literal;
            NamedVariable(name, canAssign);
        }

        public void NamedVariable(string name, bool canAssign)
        {
            OpCode getOp = OpCode.FETCH_GLOBAL, setOp = OpCode.ASSIGN_GLOBAL;
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
                                      TokenType.SLASH_EQUAL,
                                      TokenType.PERCENT_EQUAL))
            {
                var assignTokenType = PreviousToken.TokenType;

                Expression();

                // self assign ops have to be done here as they tail the previous ordered instructions
                switch (assignTokenType)
                {
                case TokenType.PLUS_EQUAL:
                    EmitOpAndBytes(getOp, (byte)argID);
                    EmitOpCode(OpCode.SWAP);
                    EmitOpCode(OpCode.ADD);
                    break;

                case TokenType.MINUS_EQUAL:
                    EmitOpAndBytes(getOp, (byte)argID);
                    EmitOpCode(OpCode.SWAP);
                    EmitOpCode(OpCode.SUBTRACT);
                    break;

                case TokenType.STAR_EQUAL:
                    EmitOpAndBytes(getOp, (byte)argID);
                    EmitOpCode(OpCode.SWAP);
                    EmitOpCode(OpCode.MULTIPLY);
                    break;

                case TokenType.SLASH_EQUAL:
                    EmitOpAndBytes(getOp, (byte)argID);
                    EmitOpCode(OpCode.SWAP);
                    EmitOpCode(OpCode.DIVIDE);
                    break;

                case TokenType.PERCENT_EQUAL:
                    EmitOpAndBytes(getOp, (byte)argID);
                    EmitOpCode(OpCode.SWAP);
                    EmitOpCode(OpCode.MODULUS);
                    break;

                case TokenType.ASSIGN:
                    break;
                }

                EmitOpAndBytes(setOp, (byte)argID);
            }
            else
            {
                EmitOpAndBytes(getOp, (byte)argID);
            }
        }

        protected Chunk EndCompile()
        {
            EmitReturn();
            return compilerStates.Pop().chunk;
        }

        protected void EmitReturn()
        {
            if (compilerStates.Peek().functionType == FunctionType.Init)
                EmitOpAndBytes(OpCode.GET_LOCAL, 0);
            else
                EmitOpCode(OpCode.NULL);

            EmitOpAndBytes(OpCode.RETURN, (byte)ReturnMode.One);
        }

        public void Consume(TokenType tokenType, string msg)
        {
            if (CurrentToken.TokenType == tokenType)
                Advance();
            else
                throw new CompilerException(msg + $" at {PreviousToken.Line}:{PreviousToken.Character} '{PreviousToken.Literal}'");
        }

        public bool Check(TokenType type)
        {
            return CurrentToken.TokenType == type;
        }

        public bool Match(TokenType type)
        {
            if (!Check(type))
                return false;
            Advance();
            return true;
        }

        private bool MatchAny(params TokenType[] type)
        {
            for (int i = 0; i < type.Length; i++)
            {
                if (!Check(type[i])) continue;

                Advance();
                return true;
            }
            return false;
        }

        public void EmitOpCode(OpCode op)
        {
            CurrentChunk.WriteSimple(op, PreviousToken.Line);
        }

        public void EmitOpAndBytes(OpCode op, params byte[] b)
        {
            EmitOpCode(op);
            EmitBytes(b);
        }

        public void Advance()
        {
            PreviousToken = CurrentToken;
            CurrentToken = tokens[tokenIndex];
            tokenIndex++;
        }

        public void PatchJump(int thenjump)
        {
            int jump = CurrentChunkInstructinCount - thenjump - 2;

            if (jump > ushort.MaxValue)
                throw new CompilerException($"Cannot jump '{jump}'. Max jump is '{ushort.MaxValue}'");

            WriteBytesAt(thenjump, (byte)((jump >> 8) & 0xff), (byte)(jump & 0xff));
        }

        public void WriteUShortAt(int at, ushort us)
        {
            WriteBytesAt(at, (byte)((us >> 8) & 0xff), (byte)(us & 0xff));
        }

        protected void WriteBytesAt(int at, params byte[] b)
        {
            for (int i = 0; i < b.Length; i++)
            {
                CurrentChunk.Instructions[at + i] = b[i];
            }
        }

        public int EmitJump(OpCode op)
        {
            EmitBytes((byte)op, 0xff, 0xff);
            return CurrentChunk.Instructions.Count - 2;
        }

        public void EmitBytes(params byte[] b)
        {
            for (int i = 0; i < b.Length; i++)
            {
                CurrentChunk.WriteByte(b[i], PreviousToken.Line);
            }
        }

        public void EmitUShort(ushort us)
        {
            EmitBytes((byte)((us >> 8) & 0xff), (byte)(us & 0xff));
        }

        public void EndScope()
        {
            var comp = CurrentCompilerState;

            comp.scopeDepth--;

            while (comp.localCount > 0 &&
                comp.locals[comp.localCount - 1].Depth > comp.scopeDepth)
            {
                if (comp.locals[comp.localCount - 1].IsCaptured)
                    EmitOpCode(OpCode.CLOSE_UPVALUE);
                else
                    EmitOpCode(OpCode.POP);

                CurrentCompilerState.localCount--;
            }
        }

        //TODO build commands should use this
        public byte ExpressionList(TokenType terminatorToken, string missingTermError)
        {
            byte argCount = 0;
            if (!Check(terminatorToken))
            {
                do
                {
                    Expression();
                    argCount++;
                    if (argCount == 255)
                        throw new CompilerException("Can't have more than 255 arguments.");

                } while (Match(TokenType.COMMA));
            }

            Consume(terminatorToken, missingTermError);
            return argCount;
        }

        public byte ArgumentList()
        {
            return ExpressionList(TokenType.CLOSE_PAREN, "Expect ')' after arguments.");
        }

        protected void EmitLoop(int loopStart)
        {
            EmitOpCode(OpCode.LOOP);
            int offset = CurrentChunk.Instructions.Count - loopStart + 2;

            if (offset > ushort.MaxValue)
                throw new CompilerException($"Cannot loop '{offset}'. Max loop is '{ushort.MaxValue}'");

            EmitBytes((byte)((offset >> 8) & 0xff), (byte)(offset & 0xff));
        }

        protected void PatchLoopExits(CompilerState.LoopState loopState)
        {
            if (loopState.loopExitPatchLocations.Count == 0)
                throw new CompilerException("Loops must contain an termination.");

            for (int i = 0; i < loopState.loopExitPatchLocations.Count; i++)
            {
                PatchJump(loopState.loopExitPatchLocations[i]);
            }
        }

        public byte ParseVariable(string errMsg)
        {
            Consume(TokenType.IDENTIFIER, errMsg);

            DeclareVariable();
            if (CurrentCompilerState.scopeDepth > 0) return 0;
            return AddStringConstant();
        }

        public void Function(string name, FunctionType functionType)
        {
            PushCompilerState(name, functionType);

            BeginScope();
            FunctionParamListOptional();

            // The body.
            Consume(TokenType.OPEN_BRACE, "Expect '{' before function body.");
            Block();

            EndFunction();
        }

        public void FunctionParamListOptional()
        {
            if (Match(TokenType.OPEN_PAREN))
            {
                // Compile the parameter list.
                //Consume(TokenType.OPEN_PAREN, "Expect '(' after function name.");
                if (!Check(TokenType.CLOSE_PAREN))
                {
                    do
                    {
                        var paramConstant = ParseVariable("Expect parameter name.");
                        DefineVariable(paramConstant);

                        //if it isn't already a constant we want one
                        IncreaseArity(AddStringConstant());
                    } while (Match(TokenType.COMMA));
                }
                Consume(TokenType.CLOSE_PAREN, "Expect ')' after parameters.");
            }
        }

        public void IncreaseArity(byte argNameConstant)
        {
            CurrentChunk.ArgumentConstantIds.Add(argNameConstant);
            if (CurrentChunk.Arity > 255)
            {
                throw new CompilerException("Can't have more than 255 parameters.");
            }

        }

        public void EndFunction()
        {
            // Create the function object.
            var comp = CurrentCompilerState;   //we need this to mark upvalues
            var function = EndCompile();
            EmitOpAndBytes(OpCode.CLOSURE, CurrentChunk.AddConstant(Value.New(function)));

            for (int i = 0; i < function.UpvalueCount; i++)
            {
                EmitBytes(comp.upvalues[i].isLocal ? (byte)1 : (byte)0);
                EmitBytes(comp.upvalues[i].index);
            }
        }

        public void Block()
        {
            while (!Check(TokenType.CLOSE_BRACE) && !Check(TokenType.EOF))
                Declaration();

            Consume(TokenType.CLOSE_BRACE, "Expect '}' after block.");
        }

        public void BeginScope()
        {
            CurrentCompilerState.scopeDepth++;
        }

        public void DefineVariable(byte global)
        {
            if (CurrentCompilerState.scopeDepth > 0)
            {
                MarkInitialised();
                return;
            }

            EmitOpAndBytes(OpCode.DEFINE_GLOBAL, global);
        }

        protected void EmitOpCodePair(OpCode op1, OpCode op2)
        {
            CurrentChunk.WriteSimple(op1, PreviousToken.Line);
            CurrentChunk.WriteSimple(op2, PreviousToken.Line);
        }

        public byte AddStringConstant() => AddCustomStringConstant((string)PreviousToken.Literal);

        public byte AddCustomStringConstant(string str) => CurrentChunk.AddConstant(Value.New(str));

        public void DeclareVariable()
        {
            var comp = CurrentCompilerState;

            if (comp.scopeDepth == 0) return;

            var declName = comp.chunk.ReadConstant(AddStringConstant()).val.asString.String;
            DeclareVariableByName(declName);
        }

        private void DeclareVariableByName(string declName)
        {
            var comp = CurrentCompilerState;
            if (comp.scopeDepth == 0) return;

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

        protected void MarkInitialised()
        {
            var comp = CurrentCompilerState;

            if (comp.scopeDepth == 0) return;
            comp.LastLocal.Depth = comp.scopeDepth;
        }

        public void FunctionDeclaration(CompilerBase compiler)
        {
            var global = ParseVariable("Expect function name.");
            MarkInitialised();

            Function(CurrentChunk.ReadConstant(global).val.asString.String, FunctionType.Function);
            DefineVariable(global);
        }

        public void VarDeclaration(CompilerBase compiler)
        {
            if (Match(TokenType.OPEN_PAREN))
                MultiVarAssignToReturns();
            else
                PlainVarDeclare();

            Consume(TokenType.END_STATEMENT, "Expect ; after variable declaration.");
        }

        private void PlainVarDeclare()
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
        }

        private void MultiVarAssignToReturns()
        {
            var varNames = new List<string>();
            do
            {
                Consume(TokenType.IDENTIFIER, "Expect identifier within multivar declaration.");
                varNames.Add((string)PreviousToken.Literal);
            } while (Match(TokenType.COMMA));

            Consume(TokenType.CLOSE_PAREN, "Expect ')' to end a multivar declaration.");
            Consume(TokenType.ASSIGN, "Expect '=' after multivar declaration.");

            //mark stack start
            EmitOpAndBytes(OpCode.RETURN, (byte)ReturnMode.MarkMultiReturnAssignStart);

            Expression();

            EmitOpAndBytes(OpCode.RETURN, (byte)ReturnMode.MarkMultiReturnAssignEnd);

            EmitOpAndBytes(OpCode.PUSH_BYTE, (byte)varNames.Count);
            EmitOpAndBytes(OpCode.VALIDATE, (byte)ValidateOp.MultiReturnMatches);

            //we don't really want to reverse these, as we want things kike (a,b) = fun return (1,2,3); ends up with 1,2
            for (int i = 0; i < varNames.Count; i++)
            {
                var varName = varNames[i];
                //do equiv of ParseVariable, DefineVariable
                DeclareVariableByName(varName);
                MarkInitialised();
                var id = AddCustomStringConstant(varName);
                DefineVariable(id);
            }
        }

        public void BlockStatement()
        {
            BeginScope();
            Block();
            EndScope();
        }

        #region Statements

        public void BlockStatement(CompilerBase obj) => BlockStatement();

        public void IfStatement(CompilerBase compiler)
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

        public void ReturnStatement(CompilerBase compiler)
        {
            if (CurrentCompilerState.functionType == FunctionType.Init)
                throw new CompilerException("Cannot return an expression from an 'init'.");

            if (Match(TokenType.OPEN_PAREN))
                MultiReturnBody();
            else
                SimpleReturnBody();

            Consume(TokenType.END_STATEMENT, "Expect ';' after return value.");
        }

        private void SimpleReturnBody()
        {
            if (Check(TokenType.END_STATEMENT))
            {
                EmitReturn();
            }
            else
            {
                Expression();
                EmitOpAndBytes(OpCode.RETURN, (byte)ReturnMode.One);
            }
        }

        private void MultiReturnBody()
        {
            EmitOpAndBytes(OpCode.RETURN, (byte)ReturnMode.Begin);
            var returnCount = ExpressionList(TokenType.CLOSE_PAREN, "Expect ')' after arguments.");
            if (returnCount == 0)
                EmitOpCode(OpCode.NULL);
            EmitOpAndBytes(OpCode.RETURN, (byte)ReturnMode.End);
        }

        public void YieldStatement(CompilerBase compiler)
        {
            EmitOpCode(OpCode.YIELD);
            Consume(TokenType.END_STATEMENT, "Expect ';' after break.");
        }

        public void BreakStatement(CompilerBase compiler)
        {
            var comp = CurrentCompilerState;
            if (comp.loopStates.Count == 0)
                throw new CompilerException("Cannot break when not inside a loop.");

            EmitOpCode(OpCode.NULL);
            int exitJump = EmitJump(OpCode.JUMP);

            Consume(TokenType.END_STATEMENT, "Expect ';' after break.");

            comp.loopStates.Peek().loopExitPatchLocations.Add(exitJump);
        }

        public void ContinueStatement(CompilerBase compiler)
        {
            var comp = CurrentCompilerState;
            if (comp.loopStates.Count == 0)
                throw new CompilerException("Cannot continue when not inside a loop.");

            EmitLoop(comp.loopStates.Peek().loopContinuePoint);

            Consume(TokenType.END_STATEMENT, "Expect ';' after break.");
        }

        public void LoopStatement(CompilerBase compiler)
        {
            ConfigurableLoopingStatement(compiler, false, false);
        }

        public void WhileStatement(CompilerBase compiler)
        {
            ConfigurableLoopingStatement(compiler, true, false);
        }

        public void ForStatement(CompilerBase compiler)
        {
            ConfigurableLoopingStatement(compiler, true, true);
        }

        protected void ConfigurableLoopingStatement(
            CompilerBase compiler,
            bool expectsLoopParethesis,
            bool expectsPreAndPostStatements)
        {
            BeginScope();

            var comp = CurrentCompilerState;
            int loopStart = CurrentChunkInstructinCount;
            var loopState = new CompilerState.LoopState();
            comp.loopStates.Push(loopState);
            loopState.loopContinuePoint = loopStart;

            if (expectsLoopParethesis)
            {
                Consume(TokenType.OPEN_PAREN, "Expect '(' after loop with conditions.");

                if (expectsPreAndPostStatements)
                {
                    ForLoopInitialisationStatement(compiler);

                    loopStart = CurrentChunkInstructinCount;
                    loopState.loopContinuePoint = loopStart;

                    if (!Match(TokenType.END_STATEMENT))
                    {
                        ForLoopCondtionStatement(loopState);
                    }

                    if (!Check(TokenType.CLOSE_PAREN))
                    {
                        int bodyJump = EmitJump(OpCode.JUMP);

                        int incrementStart = CurrentChunkInstructinCount;
                        loopState.loopContinuePoint = incrementStart;
                        Expression();
                        EmitOpCode(OpCode.POP);

                        //TODO: shouldn't you be able to omit the post loop action and have it work. this seems like it breaks it.
                        EmitLoop(loopStart);
                        loopStart = incrementStart;
                        PatchJump(bodyJump);
                    }
                }
                else
                {
                    Expression();

                    int exitJump = EmitJump(OpCode.JUMP_IF_FALSE);
                    loopState.loopExitPatchLocations.Add(exitJump);

                    EmitOpCode(OpCode.POP);
                }

                Consume(TokenType.CLOSE_PAREN, "Expect ')' after loop clauses.");
            }

            Statement();

            EmitLoop(loopStart);

            PatchLoopExits(loopState);

            EmitOpCode(OpCode.POP);

            EndScope();
        }

        protected void ForLoopCondtionStatement(CompilerState.LoopState loopState)
        {
            Expression();
            Consume(TokenType.END_STATEMENT, "Expect ';' after loop condition.");

            // Jump out of the loop if the condition is false.
            var exitJump = EmitJump(OpCode.JUMP_IF_FALSE);
            loopState.loopExitPatchLocations.Add(exitJump);
            EmitOpCode(OpCode.POP); // Condition.
        }

        protected void ForLoopInitialisationStatement(CompilerBase compiler)
        {
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
        }

        public void ThrowStatement(CompilerBase compiler)
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

        public void Unary(bool canAssign)
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

        public void Binary(bool canAssign)
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

        public void Literal(bool canAssign)
        {
            switch (PreviousToken.TokenType)
            {
            case TokenType.TRUE: EmitOpAndBytes(OpCode.PUSH_BOOL, 1); break;
            case TokenType.FALSE: EmitOpAndBytes(OpCode.PUSH_BOOL, 0); break;
            case TokenType.NULL: EmitOpCode(OpCode.NULL); break;
            case TokenType.INT:
            case TokenType.FLOAT:
                {
                    var number = (double)PreviousToken.Literal;

                    var isInt = number == System.Math.Truncate(number);

                    if (isInt && number < 255 && number >= 0)
                        EmitOpAndBytes(OpCode.PUSH_BYTE, (byte)number);
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

        public void Variable(bool canAssign)
        {
            NamedVariableFromPreviousToken(canAssign);
        }

        public void And(bool canAssign)
        {
            int endJump = EmitJump(OpCode.JUMP_IF_FALSE);

            EmitOpCode(OpCode.POP);
            ParsePrecedence(Precedence.And);

            PatchJump(endJump);
        }

        public void Or(bool canAssign)
        {
            int elseJump = EmitJump(OpCode.JUMP_IF_FALSE);
            int endJump = EmitJump(OpCode.JUMP);

            PatchJump(elseJump);
            EmitOpCode(OpCode.POP);

            ParsePrecedence(Precedence.Or);

            PatchJump(endJump);
        }

        public void Grouping(bool canAssign)
        {
            ExpressionList(TokenType.CLOSE_PAREN, "Expect ')' after expression.");
        }

        public void Call(bool canAssign)
        {
            var argCount = ArgumentList();
            EmitOpAndBytes(OpCode.CALL, argCount);
        }

        #endregion Expressions
    }
}
