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
        protected Dictionary<TokenType, Compilette> declarationCompilettes = new Dictionary<TokenType, Compilette>();
        protected Dictionary<TokenType, Compilette> statementCompilettes = new Dictionary<TokenType, Compilette>();

        public int CurrentChunkInstructinCount => CurrentChunk.Instructions.Count;
        public Chunk CurrentChunk => CurrentCompilerState.chunk;
        public CompilerState CurrentCompilerState => compilerStates.Peek();

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

        protected void PatchJump(int thenjump)
        {
            int jump = CurrentChunkInstructinCount - thenjump - 2;

            if (jump > ushort.MaxValue)
                throw new CompilerException($"Cannot jump '{jump}'. Max jump is '{ushort.MaxValue}'");

            WriteBytesAt(thenjump, (byte)((jump >> 8) & 0xff), (byte)(jump & 0xff));
        }


        protected void WriteBytesAt(int at, params byte[] b)
        {
            for (int i = 0; i < b.Length; i++)
            {
                CurrentChunk.Instructions[at + i] = b[i];
            }
        }
        protected int EmitJump(OpCode op)
        {
            EmitBytes((byte)op, 0xff, 0xff);
            return CurrentChunk.Instructions.Count - 2;
        }

        protected void EmitByte(byte b)
        {
            CurrentChunk.WriteByte(b, PreviousToken.Line);
        }

        protected void EmitBytes(params byte[] b)
        {
            for (int i = 0; i < b.Length; i++)
            {
                CurrentChunk.WriteByte(b[i], PreviousToken.Line);
            }
        }

        protected void EmitUShort(ushort us)
        {
            EmitBytes((byte)((us >> 8) & 0xff), (byte)(us & 0xff));
        }

        protected void EndScope()
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
        protected byte ArgumentList()
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
        protected byte ParseVariable(string errMsg)
        {
            Consume(TokenType.IDENTIFIER, errMsg);

            DeclareVariable();
            if (CurrentCompilerState.scopeDepth > 0) return 0;
            return AddStringConstant();
        }

        protected void Function(string name, FunctionType functionType)
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
            var comp = CurrentCompilerState;   //we need this to mark upvalues
            var function = EndCompile();
            EmitOpAndByte(OpCode.CLOSURE, CurrentChunk.AddConstant(Value.New(function)));

            for (int i = 0; i < function.UpvalueCount; i++)
            {
                EmitByte(comp.upvalues[i].isLocal ? (byte)1 : (byte)0);
                EmitByte(comp.upvalues[i].index);
            }
        }

        protected void Block()
        {
            while (!Check(TokenType.CLOSE_BRACE) && !Check(TokenType.EOF))
                Declaration();

            Consume(TokenType.CLOSE_BRACE, "Expect '}' after block.");
        }

        protected void BeginScope()
        {
            CurrentCompilerState.scopeDepth++;
        }

        protected void DefineVariable(byte global)
        {
            if (CurrentCompilerState.scopeDepth > 0)
            {
                MarkInitialised();
                return;
            }

            EmitOpAndByte(OpCode.DEFINE_GLOBAL, global);
        }
        protected void EmitOpCodePair(OpCode op1, OpCode op2)
        {
            CurrentChunk.WriteSimple(op1, PreviousToken.Line);
            CurrentChunk.WriteSimple(op2, PreviousToken.Line);
        }

        protected byte AddStringConstant() => CurrentChunk.AddConstant(Value.New((string)PreviousToken.Literal));
        protected void DeclareVariable()
        {
            var comp = CurrentCompilerState;

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


        protected void MarkInitialised()
        {
            var comp = CurrentCompilerState;

            if (comp.scopeDepth == 0) return;
            comp.locals[comp.localCount - 1].Depth = comp.scopeDepth;
        }

        protected void Declaration()
        {
            if(declarationCompilettes.TryGetValue(CurrentToken.TokenType, out var complette))
            {
                Advance();
                complette.Process();
                return;
            }

            Statement();
        }

        protected void Statement()
        {
            if (statementCompilettes.TryGetValue(CurrentToken.TokenType, out var complette))
            {
                Advance();
                complette.Process();
                return;
            }

            ExpressionStatement();
        }

        protected void ExpressionStatement()
        {
            Expression();
            Consume(TokenType.END_STATEMENT, "Expect ; after expression statement.");
            EmitOpCode(OpCode.POP);
        }

        protected void Expression()
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

        protected void PushCompilerState(string name, FunctionType functionType)
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

        protected string GetEnclosingClass()
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

        protected static void AddLocal(CompilerState comp, string name, int depth = -1)
        {
            if (comp.localCount == byte.MaxValue)
                throw new CompilerException("Too many local variables.");

            comp.locals[comp.localCount++] = new Local(name, depth);
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

        protected void NamedVariable(string name, bool canAssign)
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
                var assignTokenType = PreviousToken.TokenType;

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

        protected Chunk EndCompile()
        {
            EmitReturn();
            return compilerStates.Pop().chunk;
        }

        protected void EmitReturn()
        {
            if (compilerStates.Peek().functionType == FunctionType.Init)
                EmitOpAndByte(OpCode.GET_LOCAL, 0);
            else
                EmitOpCode(OpCode.NULL);
            
            EmitOpCode(OpCode.RETURN);
        }

        protected void Consume(TokenType tokenType, string msg)
        {
            if (CurrentToken.TokenType == tokenType)
                Advance();
            else
                throw new CompilerException(msg + $" at {PreviousToken.Line}:{PreviousToken.Character} '{PreviousToken.Literal}'");
        }

        protected bool Check(TokenType type)
        {
            return CurrentToken.TokenType == type;
        }

        protected bool Match(TokenType type)
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

        protected void EmitOpCode(OpCode op)
        {
            CurrentChunk.WriteSimple(op, PreviousToken.Line);
        }

        protected void EmitOpAndByte(OpCode op, byte b)
        {
            CurrentChunk.WriteSimple(op, PreviousToken.Line);
            CurrentChunk.WriteByte(b, PreviousToken.Line);
        }

        private void Advance()
        {
            PreviousToken = CurrentToken;
            CurrentToken = tokens[tokenIndex];
            tokenIndex++;
        }

    }
}
