using System.Collections.Generic;

namespace ULox
{
    //todo for self assign to work, we need to route it through the assign path and
    //  then dump into a grouping, once the grouping is back, we named var ourselves and then emit the math op
    public abstract class CompilerBase : ICompiler
    {
        private IndexableStack<CompilerState> compilerStates = new IndexableStack<CompilerState>();

        public Token CurrentToken { get; private set; }
        public Token PreviousToken { get; private set; }
        private List<Token> tokens;
        private int tokenIndex;

        public PrattParser PrattParser { get; private set; } = new PrattParser();
        protected Dictionary<TokenType, ICompilette> declarationCompilettes = new Dictionary<TokenType, ICompilette>();
        protected Dictionary<TokenType, ICompilette> statementCompilettes = new Dictionary<TokenType, ICompilette>();

        public int CurrentChunkInstructinCount => CurrentChunk.Instructions.Count;
        public Chunk CurrentChunk => CurrentCompilerState.chunk;
        public CompilerState CurrentCompilerState => compilerStates.Peek();

        protected CompilerBase()
        {
            Reset();
        }

        protected virtual void NoDeclarationFound()
            => Statement();

        protected virtual void NoStatementFound()
            => ExpressionStatement();

        public void AddDeclarationCompilette(ICompilette compilette)
            => declarationCompilettes[compilette.Match] = compilette;

        public void AddStatementCompilette(ICompilette compilette)
            => statementCompilettes[compilette.Match] = compilette;

        public void SetPrattRule(TokenType tt, IParseRule rule)
            => PrattParser.SetPrattRule(tt, rule);

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

        public void Declaration()
        {
            if (declarationCompilettes.TryGetValue(CurrentToken.TokenType, out var complette))
            {
                Advance();
                complette.Process(this);
                return;
            }

            NoDeclarationFound();
        }

        public void Statement()
        {
            if (statementCompilettes.TryGetValue(CurrentToken.TokenType, out var complette))
            {
                Advance();
                complette.Process(this);
                return;
            }

            NoStatementFound();
        }

        public void ExpressionStatement()
        {
            Expression();
            //todo reeated
            Consume(TokenType.END_STATEMENT, "Expect ; after expression statement.");
            EmitOpCode(OpCode.POP);
        }

        public void Expression()
            => ParsePrecedence(Precedence.Assignment);

        public void ParsePrecedence(Precedence pre)
        {
            Advance();
            var rule = PrattParser.GetRule(PreviousToken.TokenType);

            var canAssign = pre <= Precedence.Assignment;
            rule.Prefix(this, canAssign);

            while (pre <= PrattParser.GetRule(CurrentToken.TokenType).Precedence)
            {
                Advance();
                rule = PrattParser.GetRule(PreviousToken.TokenType);
                rule.Infix(this, canAssign);
            }

            if (canAssign && Match(TokenType.ASSIGN))
            {
                throw new CompilerException("Invalid assignment target.");
            }
        }

        public void PushCompilerState(string name, FunctionType functionType)
        {
            compilerStates.Push(new CompilerState(compilerStates.Peek(), functionType)
            {
                chunk = new Chunk(name),
            });

            //todo refactor out
            if (functionType == FunctionType.Method || functionType == FunctionType.Init)
                CurrentCompilerState.AddLocal("this", 0);
            else
                CurrentCompilerState.AddLocal("", 0);
        }

        //TODO refactor
        public void NamedVariable(string name, bool canAssign)
        {
            OpCode getOp = OpCode.FETCH_GLOBAL, setOp = OpCode.ASSIGN_GLOBAL;
            var argID = CurrentCompilerState.ResolveLocal(name);
            if (argID != -1)
            {
                getOp = OpCode.GET_LOCAL;
                setOp = OpCode.SET_LOCAL;
            }
            else
            {
                argID = CurrentCompilerState.ResolveUpvalue(name);
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

        public void EmitReturn()
        {
            //todo refactor out
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
            => CurrentToken.TokenType == type;

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
            => CurrentChunk.WriteSimple(op, PreviousToken.Line);

        public void EmitOpCodes(params OpCode[] ops)
        {
            for (int i = 0; i < ops.Length; i++)
                EmitOpCode(ops[i]);
        }

        public byte AddStringConstant()
            => AddCustomStringConstant((string)PreviousToken.Literal);

        public byte AddCustomStringConstant(string str)
            => CurrentChunk.AddConstant(Value.New(str));

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
            => WriteBytesAt(at, (byte)((us >> 8) & 0xff), (byte)(us & 0xff));

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
            => EmitBytes((byte)((us >> 8) & 0xff), (byte)(us & 0xff));

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
            => ExpressionList(TokenType.CLOSE_PAREN, "Expect ')' after arguments.");

        public void EmitLoop(int loopStart)
        {
            EmitOpCode(OpCode.LOOP);
            int offset = CurrentChunk.Instructions.Count - loopStart + 2;

            if (offset > ushort.MaxValue)
                throw new CompilerException($"Cannot loop '{offset}'. Max loop is '{ushort.MaxValue}'");

            EmitBytes((byte)((offset >> 8) & 0xff), (byte)(offset & 0xff));
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
            => CurrentCompilerState.scopeDepth++;

        public void DefineVariable(byte global)
        {
            if (CurrentCompilerState.scopeDepth > 0)
            {
                CurrentCompilerState.MarkInitialised();
                return;
            }

            EmitOpAndBytes(OpCode.DEFINE_GLOBAL, global);
        }

        //TODO can move to compiler state?
        public void DeclareVariable()
        {
            var comp = CurrentCompilerState;

            if (comp.scopeDepth == 0) return;

            var declName = comp.chunk.ReadConstant(AddStringConstant()).val.asString.String;
            comp.DeclareVariableByName(declName);
        }

        public void BlockStatement()
        {
            BeginScope();
            Block();
            EndScope();
        }
    }
}
