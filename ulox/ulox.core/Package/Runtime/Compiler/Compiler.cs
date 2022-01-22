using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ULox
{
    //todo for self assign to work, we need to route it through the assign path and
    //  then dump into a grouping, once the grouping is back, we named var ourselves and then emit the math op
    public class Compiler : ICompiler
    {
        private readonly IndexableStack<CompilerState> compilerStates = new IndexableStack<CompilerState>();
        private readonly PrattParserRuleSet _prattParser = new PrattParserRuleSet();

        public TokenIterator TokenIterator { get; private set; }

        public TokenType CurrentTokenType
            => TokenIterator?.CurrentToken.TokenType ?? TokenType.NONE;

        public TokenType PreviousTokenType
            => TokenIterator?.PreviousToken.TokenType ?? TokenType.NONE;

        protected Dictionary<TokenType, ICompilette> declarationCompilettes = new Dictionary<TokenType, ICompilette>();
        protected Dictionary<TokenType, ICompilette> statementCompilettes = new Dictionary<TokenType, ICompilette>();

        public int CurrentChunkInstructinCount => CurrentChunk.Instructions.Count;
        public Chunk CurrentChunk => CurrentCompilerState.chunk;
        public CompilerState CurrentCompilerState => compilerStates.Peek();

        public Compiler()
        {
            Setup();
            Reset();
        }

        private void Setup()
        {
            var _testcaseCompilette = new TestcaseCompillette();
            var _testdec = new TestDeclarationCompilette();
            _testcaseCompilette.SetTestDeclarationCompilette(_testdec);
            var _buildCompilette = new BuildCompilette();
            var _classCompiler = new ClassCompilette();
            var _diCompiletteParts = new DependencyInjectionCompilette();

            this.AddDeclarationCompilette(
                new VarDeclarationCompilette(),
                _testdec,
                _classCompiler,
                _testcaseCompilette,
                _buildCompilette
                                         );
            this.AddDeclarationCompilette(
                (TokenType.FUNCTION, Compiler.FunctionDeclaration)
                                         );

            this.AddStatementCompilette(
                new ReturnStatementCompilette(),
                new LoopStatementCompilette(),
                new WhileStatementCompilette(),
                new ForStatementCompilette()
                                       );
            this.AddStatementCompilette(
                (TokenType.IF, Compiler.IfStatement),
                (TokenType.YIELD, Compiler.YieldStatement),
                (TokenType.BREAK, Compiler.BreakStatement),
                (TokenType.CONTINUE, Compiler.ContinueStatement),
                (TokenType.OPEN_BRACE, Compiler.BlockStatement),
                (TokenType.THROW, Compiler.ThrowStatement),
                (TokenType.END_STATEMENT, Compiler.NoOpStatement),
                (TokenType.REGISTER, _diCompiletteParts.RegisterStatement),
                (TokenType.FREEZE, Compiler.FreezeStatement)
                                       );

            this.SetPrattRules(
                (TokenType.MINUS, new ActionParseRule(Compiler.Unary, Compiler.Binary, Precedence.Term)),
                (TokenType.PLUS, new ActionParseRule(null, Compiler.Binary, Precedence.Term)),
                (TokenType.SLASH, new ActionParseRule(null, Compiler.Binary, Precedence.Factor)),
                (TokenType.STAR, new ActionParseRule(null, Compiler.Binary, Precedence.Factor)),
                (TokenType.PERCENT, new ActionParseRule(null, Compiler.Binary, Precedence.Factor)),
                (TokenType.BANG, new ActionParseRule(Compiler.Unary, null, Precedence.None)),
                (TokenType.INT, new ActionParseRule(Compiler.Literal, null, Precedence.None)),
                (TokenType.FLOAT, new ActionParseRule(Compiler.Literal, null, Precedence.None)),
                (TokenType.TRUE, new ActionParseRule(Compiler.Literal, null, Precedence.None)),
                (TokenType.FALSE, new ActionParseRule(Compiler.Literal, null, Precedence.None)),
                (TokenType.NULL, new ActionParseRule(Compiler.Literal, null, Precedence.None)),
                (TokenType.BANG_EQUAL, new ActionParseRule(null, Compiler.Binary, Precedence.Equality)),
                (TokenType.EQUALITY, new ActionParseRule(null, Compiler.Binary, Precedence.Equality)),
                (TokenType.LESS, new ActionParseRule(null, Compiler.Binary, Precedence.Comparison)),
                (TokenType.LESS_EQUAL, new ActionParseRule(null, Compiler.Binary, Precedence.Comparison)),
                (TokenType.GREATER, new ActionParseRule(null, Compiler.Binary, Precedence.Comparison)),
                (TokenType.GREATER_EQUAL, new ActionParseRule(null, Compiler.Binary, Precedence.Comparison)),
                (TokenType.STRING, new ActionParseRule(Compiler.Literal, null, Precedence.None)),
                (TokenType.IDENTIFIER, new ActionParseRule(Compiler.Variable, null, Precedence.None)),
                (TokenType.AND, new ActionParseRule(null, Compiler.And, Precedence.And)),
                (TokenType.OR, new ActionParseRule(null, Compiler.Or, Precedence.Or)),
                (TokenType.OPEN_PAREN, new ActionParseRule(Compiler.Grouping, Compiler.Call, Precedence.Call)),
                (TokenType.CONTEXT_NAME_FUNC, new ActionParseRule(Compiler.FName, null, Precedence.None)),
                (TokenType.DOT, new ActionParseRule(null, Compiler.Dot, Precedence.Call)),
                (TokenType.THIS, new ActionParseRule(_classCompiler.This, null, Precedence.None)),
                (TokenType.SUPER, new ActionParseRule(_classCompiler.Super, null, Precedence.None)),
                (TokenType.CONTEXT_NAME_CLASS, new ActionParseRule(_classCompiler.CName, null, Precedence.None)),
                (TokenType.CONTEXT_NAME_TESTCASE, new ActionParseRule(_testcaseCompilette.TCName, null, Precedence.None)),
                (TokenType.CONTEXT_NAME_TESTSET, new ActionParseRule(_testdec.TSName, null, Precedence.None)),
                (TokenType.INJECT, new ActionParseRule(_diCompiletteParts.Inject, null, Precedence.Term))
                              );
        }

        public void Reset()
        {
            compilerStates.Clear();
            TokenIterator = null;

            PushCompilerState(string.Empty, FunctionType.Script);
        }

        public void AddDeclarationCompilette(ICompilette compilette)
            => declarationCompilettes[compilette.Match] = compilette;

        public void AddStatementCompilette(ICompilette compilette)
            => statementCompilettes[compilette.Match] = compilette;

        public void SetPrattRule(TokenType tt, IParseRule rule)
            => _prattParser.SetPrattRule(tt, rule);

        public void EmitOpCode(OpCode op)
            => CurrentChunk.WriteSimple(op, TokenIterator.PreviousToken.Line);

        public void EmitOpCodes(params OpCode[] ops)
        {
            for (int i = 0; i < ops.Length; i++)
                EmitOpCode(ops[i]);
        }

        public byte AddStringConstant()
            => AddCustomStringConstant((string)TokenIterator.PreviousToken.Literal);

        public void AddConstantAndWriteOp(Value value)
            => CurrentChunk.AddConstantAndWriteInstruction(value, TokenIterator.PreviousToken.Line);

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
            => _prattParser.ParsePrecedence(this, pre);

        public void ConsumeEndStatement([CallerMemberName] string after = default)
            => TokenIterator.Consume(TokenType.END_STATEMENT, $"Expect ; after {after}.");

        public void PushCompilerState(string name, FunctionType functionType)
        {
            var newCompState = new CompilerState(compilerStates.Peek(), functionType)
            {
                chunk = new Chunk(name),
            };
            compilerStates.Push(newCompState);

            AfterCompilerStatePushed();
        }

        protected void AfterCompilerStatePushed()
        {
            var functionType = CurrentCompilerState.functionType;

            //calls have local 0 as a reference to the closure but are not able to ref it themselves.
            if (functionType == FunctionType.Script || functionType == FunctionType.Function)
                CurrentCompilerState.AddLocal("", 0);

            if (functionType == FunctionType.Method || functionType == FunctionType.Init)
                CurrentCompilerState.AddLocal("this", 0);
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
            if (CurrentCompilerState.functionType == FunctionType.Init)
                EmitOpAndBytes(OpCode.GET_LOCAL, 0);
            else
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
                throw new CompilerException("Can't have more than 255 parameters.");
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

        public static void Binary(Compiler compiler, bool canAssign)
        {
            TokenType operatorType = compiler.PreviousTokenType;

            // Compile the right operand.
            var rule = compiler._prattParser.GetRule(operatorType);
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

        public static void FreezeStatement(Compiler compiler)
        {
            compiler.Expression();
            compiler.EmitOpCode(OpCode.FREEZE);
            compiler.ConsumeEndStatement();
        }

        public static void Dot(Compiler compiler, bool canAssign)
        {
            compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect property name after '.'.");
            byte name = compiler.AddStringConstant();

            if (canAssign && compiler.TokenIterator.Match(TokenType.ASSIGN))
            {
                compiler.Expression();
                compiler.EmitOpAndBytes(OpCode.SET_PROPERTY, name);
            }
            else if (compiler.TokenIterator.Match(TokenType.OPEN_PAREN))
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

        public static void FName(Compiler compiler, bool canAssign)
        {
            var fname = compiler.CurrentChunk.Name;
            compiler.AddConstantAndWriteOp(Value.New(fname));
        }

        public static void ThrowStatement(Compiler compiler)
        {
            if (!compiler.TokenIterator.Check(TokenType.END_STATEMENT))
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

        public static void ContinueStatement(Compiler compiler)
        {
            var comp = compiler.CurrentCompilerState;
            if (comp.LoopStates.Count == 0)
                throw new CompilerException("Cannot continue when not inside a loop.");

            compiler.EmitLoop(comp.LoopStates.Peek().loopContinuePoint);

            compiler.ConsumeEndStatement();
        }

        public static void IfStatement(Compiler compiler)
        {
            compiler.TokenIterator.Consume(TokenType.OPEN_PAREN, "Expect '(' after if.");
            compiler.Expression();
            compiler.TokenIterator.Consume(TokenType.CLOSE_PAREN, "Expect ')' after if.");

            int thenjump = compiler.EmitJump(OpCode.JUMP_IF_FALSE);
            compiler.EmitOpCode(OpCode.POP);

            compiler.Statement();

            int elseJump = compiler.EmitJump(OpCode.JUMP);

            compiler.PatchJump(thenjump);
            compiler.EmitOpCode(OpCode.POP);

            if (compiler.TokenIterator.Match(TokenType.ELSE)) compiler.Statement();

            compiler.PatchJump(elseJump);
        }

        public static void BreakStatement(Compiler compiler)
        {
            var comp = compiler.CurrentCompilerState;
            if (comp.LoopStates.Count == 0)
                throw new CompilerException("Cannot break when not inside a loop.");

            compiler.EmitOpCode(OpCode.NULL);
            int exitJump = compiler.EmitJump(OpCode.JUMP);

            compiler.ConsumeEndStatement();

            comp.LoopStates.Peek().loopExitPatchLocations.Add(exitJump);
        }

        public static void YieldStatement(Compiler compiler)
        {
            compiler.EmitOpCode(OpCode.YIELD);

            compiler.ConsumeEndStatement();
        }

        public static void BlockStatement(Compiler compiler)
            => compiler.BlockStatement();

        public static void FunctionDeclaration(Compiler compiler)
        {
            var global = compiler.ParseVariable("Expect function name.");
            compiler.CurrentCompilerState.MarkInitialised();

            compiler.Function(compiler.CurrentChunk.ReadConstant(global).val.asString.String, FunctionType.Function);
            compiler.DefineVariable(global);
        }

        public static void NoOpStatement(Compiler compiler)
        {
        }

        public static void Unary(Compiler compiler, bool canAssign)
        {
            var op = compiler.PreviousTokenType;

            compiler.ParsePrecedence(Precedence.Unary);

            switch (op)
            {
            case TokenType.MINUS: compiler.EmitOpCode(OpCode.NEGATE); break;
            case TokenType.BANG: compiler.EmitOpCode(OpCode.NOT); break;
            default:
                break;
            }
        }

        public static void Literal(Compiler compiler, bool canAssign)
        {
            switch (compiler.PreviousTokenType)
            {
            case TokenType.TRUE: compiler.EmitOpAndBytes(OpCode.PUSH_BOOL, 1); break;
            case TokenType.FALSE: compiler.EmitOpAndBytes(OpCode.PUSH_BOOL, 0); break;
            case TokenType.NULL: compiler.EmitOpCode(OpCode.NULL); break;
            case TokenType.INT:
            case TokenType.FLOAT:
                {
                    var number = (double)compiler.TokenIterator.PreviousToken.Literal;

                    var isInt = number == System.Math.Truncate(number);

                    if (isInt && number < 255 && number >= 0)
                        compiler.EmitOpAndBytes(OpCode.PUSH_BYTE, (byte)number);
                    else
                        //todo push to compiler
                        compiler.AddConstantAndWriteOp(Value.New(number));
                }
                break;

            case TokenType.STRING:
                {
                    var str = (string)compiler.TokenIterator.PreviousToken.Literal;
                    compiler.AddConstantAndWriteOp(Value.New(str));
                }
                break;
            }
        }

        public static void Variable(Compiler compiler, bool canAssign)
        {
            var name = (string)compiler.TokenIterator.PreviousToken.Literal;
            compiler.NamedVariable(name, canAssign);
        }

        public static void And(Compiler compiler, bool canAssign)
        {
            int endJump = compiler.EmitJump(OpCode.JUMP_IF_FALSE);

            compiler.EmitOpCode(OpCode.POP);
            compiler.ParsePrecedence(Precedence.And);

            compiler.PatchJump(endJump);
        }

        public static void Or(Compiler compiler, bool canAssign)
        {
            int elseJump = compiler.EmitJump(OpCode.JUMP_IF_FALSE);
            int endJump = compiler.EmitJump(OpCode.JUMP);

            compiler.PatchJump(elseJump);
            compiler.EmitOpCode(OpCode.POP);

            compiler.ParsePrecedence(Precedence.Or);

            compiler.PatchJump(endJump);
        }

        public static void Grouping(Compiler compiler, bool canAssign)
        {
            compiler.ExpressionList(TokenType.CLOSE_PAREN, "Expect ')' after expression.");
        }

        public static void Call(Compiler compiler, bool canAssign)
        {
            var argCount = compiler.ArgumentList();
            compiler.EmitOpAndBytes(OpCode.CALL, argCount);
        }
    }
}
