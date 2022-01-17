using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ULox
{
    //todo for self assign to work, we need to route it through the assign path and
    //  then dump into a grouping, once the grouping is back, we named var ourselves and then emit the math op
    public abstract class CompilerBase : ICompiler
    {
        private IndexableStack<CompilerState> compilerStates = new IndexableStack<CompilerState>();

        public TokenIterator TokenIterator { get; private set; }
        public TokenType CurrentTokenType
            => TokenIterator?.CurrentToken.TokenType ?? TokenType.NONE;
        public TokenType PreviousTokenType
            => TokenIterator?.PreviousToken.TokenType ?? TokenType.NONE;

        public PrattParserRuleSet PrattParser { get; private set; } = new PrattParserRuleSet();
        protected Dictionary<TokenType, ICompilette> declarationCompilettes = new Dictionary<TokenType, ICompilette>();
        protected Dictionary<TokenType, ICompilette> statementCompilettes = new Dictionary<TokenType, ICompilette>();

        public int CurrentChunkInstructinCount => CurrentChunk.Instructions.Count;
        public Chunk CurrentChunk => CurrentCompilerState.chunk;
        public CompilerState CurrentCompilerState => compilerStates.Peek();

        protected CompilerBase() 
            => Reset();

        public void Reset()
        {
            compilerStates = new IndexableStack<CompilerState>();
            TokenIterator = null;

            PushCompilerState(string.Empty, FunctionType.Script);
        }

        public void AddDeclarationCompilette(ICompilette compilette)
            => declarationCompilettes[compilette.Match] = compilette;

        public void AddStatementCompilette(ICompilette compilette)
            => statementCompilettes[compilette.Match] = compilette;

        public void SetPrattRule(TokenType tt, IParseRule rule)
            => PrattParser.SetPrattRule(tt, rule);



        public void EmitOpCode(OpCode op)
            => CurrentChunk.WriteSimple(op, TokenIterator.PreviousToken.Line);

        public void EmitOpCodes(params OpCode[] ops)
        {
            for (int i = 0; i < ops.Length; i++)
                EmitOpCode(ops[i]);
        }

        public byte AddStringConstant()
            => AddCustomStringConstant((string)TokenIterator.PreviousToken.Literal);

        public byte AddCustomStringConstant(string str)
            => CurrentChunk.AddConstant(Value.New(str));

        public void EmitOpAndBytes(OpCode op, params byte[] b)
        {
            EmitOpCode(op);
            EmitBytes(b);
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
                CurrentChunk.WriteByte(b[i], TokenIterator.PreviousToken.Line);
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

        public Chunk Compile(List<Token> inTokens)
        {
            TokenIterator = new TokenIterator(inTokens);
            TokenIterator.Advance();

            while (CurrentTokenType != TokenType.EOF)
            {
                Declaration();
            }

            return EndCompile();
        }

        public void Declaration()
        {
            if (declarationCompilettes.TryGetValue(CurrentTokenType, out var complette))
            {
                TokenIterator.Advance();
                complette.Process(this);
                return;
            }

            NoDeclarationFound();
        }

        protected virtual void NoDeclarationFound()
            => Statement();

        public void Statement()
        {
            if (statementCompilettes.TryGetValue(CurrentTokenType, out var complette))
            {
                TokenIterator.Advance();
                complette.Process(this);
                return;
            }

            NoStatementFound();
        }

        protected virtual void NoStatementFound()
            => ExpressionStatement();

        public void ExpressionStatement()
        {
            Expression();
            ConsumeEndStatement();
            EmitOpCode(OpCode.POP);
        }

        public void Expression()
            => ParsePrecedence(Precedence.Assignment);

        public void ParsePrecedence(Precedence pre)
        {
            TokenIterator.Advance();
            var rule = PrattParser.GetRule(PreviousTokenType);

            var canAssign = pre <= Precedence.Assignment;
            rule.Prefix(this, canAssign);

            while (pre <= PrattParser.GetRule(CurrentTokenType).Precedence)
            {
                TokenIterator.Advance();
                rule = PrattParser.GetRule(PreviousTokenType);
                rule.Infix(this, canAssign);
            }

            if (canAssign && TokenIterator.Match(TokenType.ASSIGN))
            {
                throw new CompilerException("Invalid assignment target.");
            }
        }

        public void ConsumeEndStatement([CallerMemberName] string after = default)
        {
            TokenIterator.Consume(TokenType.END_STATEMENT, $"Expect ; after {after}.");
        }

        public void PushCompilerState(string name, FunctionType functionType)
        {
            var newCompState = new CompilerState(compilerStates.Peek(), functionType)
            {
                chunk = new Chunk(name),
            };
            compilerStates.Push(newCompState);

            AfterCompilerStatePushed();
        }

        protected virtual void AfterCompilerStatePushed()
        {
            var functionType = CurrentCompilerState.functionType;

            //calls have local 0 as a reference to the closure but are not able to ref it themselves.
            if (functionType == FunctionType.Script || functionType == FunctionType.Function)
                CurrentCompilerState.AddLocal("", 0);
        }

        public void NamedVariable(string name, bool canAssign)
        {
            (var getOp, var setOp, var argId) = ResolveNameLookupOpCode(name);

            if (!canAssign)
            {
                EmitOpAndBytes(getOp, argId);
                return;
            }

            if (TokenIterator.Match(TokenType.ASSIGN))
            {
                Expression();
                EmitOpAndBytes(setOp, argId);
                return;
            }

            if (HandleCompoundAssignToken(getOp, setOp, argId))
                return;

            EmitOpAndBytes(getOp, argId);
        }

        private bool HandleCompoundAssignToken(OpCode getOp, OpCode setOp, byte argId)
        {
            if (TokenIterator.MatchAny(TokenType.PLUS_EQUAL,
                         TokenType.MINUS_EQUAL,
                         TokenType.STAR_EQUAL,
                         TokenType.SLASH_EQUAL,
                         TokenType.PERCENT_EQUAL))
            {
                var assignTokenType = PreviousTokenType;

                Expression();

                //expand the compound op
                EmitOpAndBytes(getOp, argId);
                EmitOpCode(OpCode.SWAP);

                // self assign ops have to be done here as they tail the previous ordered instructions
                switch (assignTokenType)
                {
                case TokenType.PLUS_EQUAL:
                    EmitOpCode(OpCode.ADD);
                    break;

                case TokenType.MINUS_EQUAL:
                    EmitOpCode(OpCode.SUBTRACT);
                    break;

                case TokenType.STAR_EQUAL:
                    EmitOpCode(OpCode.MULTIPLY);
                    break;

                case TokenType.SLASH_EQUAL:
                    EmitOpCode(OpCode.DIVIDE);
                    break;

                case TokenType.PERCENT_EQUAL:
                    EmitOpCode(OpCode.MODULUS);
                    break;
                }

                EmitOpAndBytes(setOp, argId);
                return true;
            }
            return false;
        }

        private (OpCode getOp, OpCode setOp, byte argId) ResolveNameLookupOpCode(string name)
        {
            var getOp = OpCode.FETCH_GLOBAL;
            var setOp = OpCode.ASSIGN_GLOBAL;
            var argId = CurrentCompilerState.ResolveLocal(name);
            if (argId != -1)
            {
                getOp = OpCode.GET_LOCAL;
                setOp = OpCode.SET_LOCAL;
            }
            else
            {
                argId = CurrentCompilerState.ResolveUpvalue(name);
                if (argId != -1)
                {
                    getOp = OpCode.GET_UPVALUE;
                    setOp = OpCode.SET_UPVALUE;
                }
                else
                {
                    argId = CurrentChunk.AddConstant(Value.New(name));
                }
            }

            return (getOp, setOp, (byte)argId);
        }

        protected Chunk EndCompile()
        {
            EmitReturn();
            return compilerStates.Pop().chunk;
        }

        public void EmitReturn()
        {
            PreEmptyReturnEmit();

            EmitOpAndBytes(OpCode.RETURN, (byte)ReturnMode.One);
        }

        protected virtual void PreEmptyReturnEmit()
        {
            EmitOpCode(OpCode.NULL);
        }

        //TODO build commands should use this
        public byte ExpressionList(TokenType terminatorToken, string missingTermError)
        {
            byte argCount = 0;
            if (!TokenIterator.Check(terminatorToken))
            {
                do
                {
                    Expression();
                    argCount++;
                    if (argCount == 255)
                        throw new CompilerException("Can't have more than 255 arguments.");
                } while (TokenIterator.Match(TokenType.COMMA));
            }

            TokenIterator.Consume(terminatorToken, missingTermError);
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
            TokenIterator.Consume(TokenType.IDENTIFIER, errMsg);

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
            TokenIterator.Consume(TokenType.OPEN_BRACE, "Expect '{' before function body.");
            Block();

            EndFunction();
        }

        public void FunctionParamListOptional()
        {
            if (TokenIterator.Match(TokenType.OPEN_PAREN))
            {
                // Compile the parameter list.
                //Consume(TokenType.OPEN_PAREN, "Expect '(' after function name.");
                if (!TokenIterator.Check(TokenType.CLOSE_PAREN))
                {
                    do
                    {
                        var paramConstant = ParseVariable("Expect parameter name.");
                        DefineVariable(paramConstant);

                        //if it isn't already a constant we want one
                        IncreaseArity(AddStringConstant());
                    } while (TokenIterator.Match(TokenType.COMMA));
                }
                TokenIterator.Consume(TokenType.CLOSE_PAREN, "Expect ')' after parameters.");
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
            while (!TokenIterator.Check(TokenType.CLOSE_BRACE) 
                && !TokenIterator.Check(TokenType.EOF))
                Declaration();

            TokenIterator.Consume(TokenType.CLOSE_BRACE, "Expect '}' after block.");
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
