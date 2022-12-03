﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ULox
{
    //todo for self assign to work, we need to route it through the assign path and
    //  then dump into a grouping, once the grouping is back, we named var ourselves and then emit the math op
    public sealed class Compiler : ICompiler
    {
        private readonly IndexableStack<CompilerState> compilerStates = new IndexableStack<CompilerState>();
        private readonly PrattParserRuleSet _prattParser = new PrattParserRuleSet();

        public TokenIterator TokenIterator { get; private set; }

        public TokenType CurrentTokenType
            => TokenIterator?.CurrentToken.TokenType ?? TokenType.NONE;

        public TokenType PreviousTokenType
            => TokenIterator?.PreviousToken.TokenType ?? TokenType.NONE;

        private Dictionary<TokenType, ICompilette> declarationCompilettes = new Dictionary<TokenType, ICompilette>();
        private Dictionary<TokenType, ICompilette> statementCompilettes = new Dictionary<TokenType, ICompilette>();
        private List<Chunk> _allChunks = new List<Chunk>();

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
            var _classCompiler = TypeCompilette.CreateClassCompilette();

            this.AddDeclarationCompilette(
                new VarDeclarationCompilette(),
                _testdec,
                _classCompiler,
                _testcaseCompilette,
                _buildCompilette,
                TypeCompilette.CreateDateCompilette());

            this.AddDeclarationCompilette(
                (TokenType.FUNCTION, FunctionDeclaration));

            this.AddStatementCompilette(
                new ReturnStatementCompilette(),
                new LoopStatementCompilette(),
                new WhileStatementCompilette(),
                new ForStatementCompilette());

            this.AddStatementCompilette(
                (TokenType.IF, IfStatement),
                (TokenType.YIELD, YieldStatement),
                (TokenType.BREAK, BreakStatement),
                (TokenType.CONTINUE, ContinueStatement),
                (TokenType.OPEN_BRACE, BlockStatement),
                (TokenType.THROW, ThrowStatement),
                (TokenType.END_STATEMENT, NoOpStatement),
                (TokenType.REGISTER, RegisterStatement),
                (TokenType.FREEZE, FreezeStatement),
                (TokenType.EXPECT, ExpectStatement),
                (TokenType.MATCH, MatchStatement),
                (TokenType.LABEL, LabelStatement),
                (TokenType.GOTO, GotoStatement));

            this.SetPrattRules(
                (TokenType.MINUS, new ActionParseRule(Unary, Binary, Precedence.Term)),
                (TokenType.PLUS, new ActionParseRule(null, Binary, Precedence.Term)),
                (TokenType.SLASH, new ActionParseRule(null, Binary, Precedence.Factor)),
                (TokenType.STAR, new ActionParseRule(null, Binary, Precedence.Factor)),
                (TokenType.PERCENT, new ActionParseRule(null, Binary, Precedence.Factor)),
                (TokenType.BANG, new ActionParseRule(Unary, null, Precedence.None)),
                (TokenType.NUMBER, new ActionParseRule(Literal, null, Precedence.None)),
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
                (TokenType.OPEN_BRACKET, new ActionParseRule(BracketCreate, BracketSubScript, Precedence.Call)),
                (TokenType.OPEN_BRACE, new ActionParseRule(BraceCreateDynamic, null, Precedence.Call)),
                (TokenType.DOT, new ActionParseRule(null, Dot, Precedence.Call)),
                (TokenType.THIS, new ActionParseRule(_classCompiler.This, null, Precedence.None)),
                (TokenType.CONTEXT_NAME_CLASS, new ActionParseRule(_classCompiler.CName, null, Precedence.None)),
                (TokenType.CONTEXT_NAME_TESTCASE, new ActionParseRule(_testcaseCompilette.TCName, null, Precedence.None)),
                (TokenType.CONTEXT_NAME_TESTSET, new ActionParseRule(_testdec.TSName, null, Precedence.None)),
                (TokenType.INJECT, new ActionParseRule(Inject, null, Precedence.Term)),
                (TokenType.TYPEOF, new ActionParseRule(TypeOf, null, Precedence.Term)),
                (TokenType.MEETS, new ActionParseRule(null, Meets, Precedence.Comparison)),
                (TokenType.SIGNS, new ActionParseRule(null, Signs, Precedence.Comparison)),
                (TokenType.FUNCTION, new ActionParseRule(FunExp, null, Precedence.Call)),
                (TokenType.COUNT_OF, new ActionParseRule(CountOf, null, Precedence.None))
                              );
        }

        public void Reset()
        {
            compilerStates.Clear();
            TokenIterator = null;
            _allChunks.Clear();
        }

        public void ThrowCompilerException(string msg)
        {
            throw new CompilerException(msg, TokenIterator.PreviousToken, $"chunk '{CurrentChunk.GetLocationString()}'");
        }

        public void AddDeclarationCompilette(ICompilette compilette)
            => declarationCompilettes[compilette.Match] = compilette;

        public void AddStatementCompilette(ICompilette compilette)
            => statementCompilettes[compilette.Match] = compilette;

        public void SetPrattRule(TokenType tt, IParseRule rule)
            => _prattParser.SetPrattRule(tt, rule);

        public void EmitOpCode(OpCode op)
            => CurrentChunk.WriteSimple(op, TokenIterator.PreviousToken.Line);

        public void EmitNULL()
            => EmitOpCode(OpCode.NULL);

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
            {
                ThrowCompilerException($"Cannot jump '{jump}'. Max jump is '{ushort.MaxValue}'");
                return;
            }

            WriteBytesAt(thenjump, (byte)((jump >> 8) & 0xff), (byte)(jump & 0xff));
        }

        public void WriteUShortAt(int at, ushort us)
            => WriteBytesAt(at, (byte)((us >> 8) & 0xff), (byte)(us & 0xff));

        private void WriteBytesAt(int at, params byte[] b)
        {
            for (int i = 0; i < b.Length; i++)
            {
                CurrentChunk.Instructions[at + i] = b[i];
            }
        }

        public int EmitJumpIf()
        {
            EmitBytes((byte)OpCode.JUMP_IF_FALSE, 0xff, 0xff);
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

        public CompiledScript Compile(List<Token> tokens, Script script)
        {
            TokenIterator = new TokenIterator(tokens, script.Name);
            TokenIterator.Advance();

            PushCompilerState(string.Empty, FunctionType.Script);

            while (CurrentTokenType != TokenType.EOF)
            {
                Declaration();
            }

            var topChunk = EndCompile();
            return new CompiledScript(topChunk, script.GetHashCode(), _allChunks.GetRange(0, _allChunks.Count));
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

        private void NoDeclarationFound()
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

        private void NoStatementFound()
            => ExpressionStatement();

        public void ExpressionStatement()
        {
            Expression();
            ConsumeEndStatement();
            EmitOpCode(OpCode.POP);
        }

        public void Expression()
        {
            try
            {
                ParsePrecedence(Precedence.Assignment);
            }
            catch (UloxException) { throw; }
            catch (Exception)
            {
                ThrowCompilerException("Expected to compile Expression, but encountered error");
            }
        }

        public void ParsePrecedence(Precedence pre)
            => _prattParser.ParsePrecedence(this, pre);

        public void ConsumeEndStatement([CallerMemberName] string after = default)
            => TokenIterator.Consume(TokenType.END_STATEMENT, $"Expect ; after {after}.");

        public void PushCompilerState(string name, FunctionType functionType)
        {
            var newCompState = new CompilerState(compilerStates.Peek(), functionType)
            {
                chunk = new Chunk(name, TokenIterator?.SourceName, functionType),
            };
            compilerStates.Push(newCompState);

            AfterCompilerStatePushed();
        }

        private void AfterCompilerStatePushed()
        {
            var functionType = CurrentCompilerState.functionType;

            if (functionType == FunctionType.Method
                || functionType == FunctionType.LocalMethod
                || functionType == FunctionType.Init)
            {
                CurrentCompilerState.AddLocal(this, "this", 0);
            }
            else
            {
                //calls have local 0 as a reference to the closure but are not able to ref it themselves.
                CurrentCompilerState.AddLocal(this, "", 0);
            }
        }

        public void NamedVariable(string name, bool canAssign)
        {
            (var getOp, var setOp, var argId) = ResolveNameLookupOpCode(name);

            ConfirmAccess(getOp, setOp, name);

            if (!canAssign)
            {
                EmitOpAndBytes(getOp, argId);
                return;
            }

            if (TokenIterator.Match(TokenType.ASSIGN))
            {
                Expression();
                ConfirmWrite(name, argId);

                EmitOpAndBytes(setOp, argId);
                return;
            }

            if (HandleCompoundAssignToken(getOp, setOp, argId))
            {
                ConfirmWrite(name, argId);
                return;
            }

            EmitOpAndBytes(getOp, argId);
        }

        private void ConfirmWrite(string name, byte argId)
        {
            if (CurrentCompilerState.functionType == FunctionType.PureFunction)
            {
                if (argId <= CurrentCompilerState.chunk.Arity)
                    ThrowCompilerException($"Attempted to write to function param '{name}', this is not allowed in a 'pure' function");
            }
        }

        private void ConfirmAccess(OpCode getOp, OpCode setOp, string name)
        {
            if (IsFunctionLocal())
            {
                if (getOp != OpCode.GET_LOCAL
                    || setOp != OpCode.SET_LOCAL)
                    ThrowCompilerException($"Identifiier '{name}' could not be found locally in local function '{CurrentCompilerState.chunk.Name}'");
            }
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

        public (OpCode getOp, OpCode setOp, byte argId) ResolveNameLookupOpCode(string name)
        {
            var getOp = OpCode.FETCH_GLOBAL;
            var setOp = OpCode.ASSIGN_GLOBAL;
            var argId = CurrentCompilerState.ResolveLocal(this, name);
            if (argId != -1)
            {
                getOp = OpCode.GET_LOCAL;
                setOp = OpCode.SET_LOCAL;
            }
            else
            {
                argId = CurrentCompilerState.ResolveUpvalue(this, name);
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

        private bool IsFunctionLocal()
        {
            var ft = CurrentCompilerState.functionType;
            return ft == FunctionType.LocalFunction
                || ft == FunctionType.LocalMethod
                || ft == FunctionType.PureFunction;
        }

        private Chunk EndCompile()
        {
            EmitReturn();
            var returnChunk = compilerStates.Pop().chunk;
            _allChunks.Add(returnChunk);
            return returnChunk;
        }

        public void EmitReturn()
        {
            PreEmptyReturnEmit();

            EmitOpAndBytes(OpCode.RETURN, (byte)ReturnMode.One);
        }

        private void PreEmptyReturnEmit()
        {
            if (CurrentCompilerState.functionType == FunctionType.Init)
                EmitOpAndBytes(OpCode.GET_LOCAL, 0);
            else
                EmitNULL();
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
                        ThrowCompilerException($"Can't have more than 255 arguments.");
                } while (TokenIterator.Match(TokenType.COMMA));
            }

            TokenIterator.Consume(terminatorToken, missingTermError);
            return argCount;
        }

        public byte ArgumentList()
            => ExpressionList(TokenType.CLOSE_PAREN, "Expect ')' after arguments.");

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
            VariableNameListDeclareOptional(() => IncreaseArity(AddStringConstant()));
        }

        public byte VariableNameListDeclareOptional(Action postDefinePerVar)
        {
            byte argCount = 0;
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
                        postDefinePerVar?.Invoke();
                        argCount++;
                    } while (TokenIterator.Match(TokenType.COMMA));
                }
                TokenIterator.Consume(TokenType.CLOSE_PAREN, "Expect ')' after parameters.");
            }
            return argCount;
        }

        public void IncreaseArity(byte argNameConstant)
        {
            CurrentChunk.ArgumentConstantIds.Add(argNameConstant);
            if (CurrentChunk.Arity > 255)
                ThrowCompilerException($"Can't have more than 255 parameters.");
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

        public void DeclareAndDefineCustomVariable(string varName)
        {
            //do equiv of ParseVariable, DefineVariable
            CurrentCompilerState.DeclareVariableByName(this, varName);
            CurrentCompilerState.MarkInitialised();
            var id = AddCustomStringConstant(varName);
            DefineVariable(id);
        }

        //TODO can move to compiler state?
        public void DeclareVariable()
        {
            var comp = CurrentCompilerState;

            if (comp.scopeDepth == 0) return;

            var declName = comp.chunk.ReadConstant(AddStringConstant()).val.asString.String;
            comp.DeclareVariableByName(this, declName);
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

        public static void ExpectStatement(Compiler compiler)
        {
            do
            {
                compiler.Expression();
                if (compiler.TokenIterator.Match(TokenType.COLON))
                {
                    compiler.Expression();
                }
                else
                {
                    compiler.EmitNULL();
                }
                compiler.EmitOpCode(OpCode.EXPECT);
            }
            while (compiler.TokenIterator.Match(TokenType.COMMA));


            compiler.ConsumeEndStatement();
        }

        private static void BraceCreateDynamic(Compiler compiler, bool arg2)
        {
            if (compiler.TokenIterator.Match(TokenType.COLON)
                  && compiler.TokenIterator.Match(TokenType.CLOSE_BRACE))
            {
                compiler.EmitOpAndBytes(OpCode.NATIVE_TYPE, (byte)NativeType.Dynamic);
            }
            else if (compiler.TokenIterator.Check(TokenType.IDENTIFIER))
            {
                compiler.EmitOpAndBytes(OpCode.NATIVE_TYPE, (byte)NativeType.Dynamic);

                while (!compiler.TokenIterator.Match(TokenType.CLOSE_BRACE))
                {
                    //we need to copy the dynamic inst
                    compiler.EmitOpCode(OpCode.DUPLICATE);
                    compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect identifier.");
                    //add the constant
                    var identConstantID = compiler.AddStringConstant();
                    //read the colon
                    compiler.TokenIterator.Consume(TokenType.COLON, "Expect ':' after identifiier.");
                    //do expression
                    compiler.Expression();
                    //we need a set property
                    compiler.EmitOpAndBytes(OpCode.SET_PROPERTY, identConstantID);
                    compiler.EmitOpCode(OpCode.POP);

                    //if comma consume
                    compiler.TokenIterator.Match(TokenType.COMMA);
                }
            }
            else
            {
                compiler.ThrowCompilerException("Expect identifier or ':' after '{'.");
            }
        }

        public static void TypeOf(Compiler compiler, bool canAssign)
        {
            compiler.TokenIterator.Consume(TokenType.OPEN_PAREN, "Expect '(' after typeof.");
            compiler.Expression();
            compiler.TokenIterator.Consume(TokenType.CLOSE_PAREN, "Expect ')' after typeof.");
            compiler.EmitOpCode(OpCode.TYPEOF);
        }

        public static void BracketCreate(Compiler compiler, bool canAssign)
        {
            if (compiler.TokenIterator.Match(TokenType.COLON)
                && compiler.TokenIterator.Match(TokenType.CLOSE_BRACKET))
            {
                compiler.EmitOpAndBytes(OpCode.NATIVE_TYPE, (byte)NativeType.Map);
                return;
            }

            var nativeTypeInstruction = compiler.CurrentChunkInstructinCount;
            compiler.EmitOpAndBytes(OpCode.NATIVE_TYPE, (byte)NativeType.List);

            var firstLoop = true;
            var isList = true;

            while (!compiler.TokenIterator.Check(TokenType.CLOSE_BRACKET))
            {
                compiler.EmitOpCode(OpCode.DUPLICATE);
                compiler.Expression();

                if (firstLoop
                    && compiler.TokenIterator.Check(TokenType.COLON))
                {
                    //switch to map
                    isList = false;
                    compiler.WriteBytesAt(nativeTypeInstruction, (byte)OpCode.NATIVE_TYPE, (byte)NativeType.Map);
                }

                if (isList)
                {
                    var addNameID = compiler.AddCustomStringConstant("Add");
                    compiler.EmitOpAndBytes(OpCode.INVOKE, addNameID, 1);
                }
                else
                {
                    compiler.TokenIterator.Consume(TokenType.COLON, "Expect ':' after key");
                    compiler.Expression();
                    compiler.EmitOpCode(OpCode.SET_INDEX);
                }
                compiler.EmitOpCode(OpCode.POP);

                compiler.TokenIterator.Match(TokenType.COMMA);
                firstLoop = false;
            }

            compiler.TokenIterator.Consume(TokenType.CLOSE_BRACKET, $"Expect ']' after list.");
        }

        public static void BracketSubScript(Compiler compiler, bool canAssign)
        {
            compiler.Expression();
            compiler.TokenIterator.Consume(TokenType.CLOSE_BRACKET, "Expect close of bracket after open and expression");
            if (canAssign && compiler.TokenIterator.Match(TokenType.ASSIGN))
            {
                compiler.Expression();
                compiler.EmitOpCode(OpCode.SET_INDEX);
            }
            else
            {
                compiler.EmitOpCode(OpCode.GET_INDEX);
            }
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
                compiler.EmitOpAndBytes(OpCode.INVOKE, name, argCount);
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
                compiler.EmitNULL();
            }

            compiler.ConsumeEndStatement();
            compiler.EmitOpCode(OpCode.THROW);
        }

        public static void ContinueStatement(Compiler compiler)
        {
            var comp = compiler.CurrentCompilerState;
            if (comp.LoopStates.Count == 0)
                compiler.ThrowCompilerException($"Cannot continue when not inside a loop.");

            compiler.EmitGoto(comp.LoopStates.Peek().ContinueLabelID);

            compiler.ConsumeEndStatement();
        }

        public static void IfStatement(Compiler compiler)
        {
            compiler.TokenIterator.Consume(TokenType.OPEN_PAREN, "Expect '(' after if.");
            compiler.Expression();
            compiler.TokenIterator.Consume(TokenType.CLOSE_PAREN, "Expect ')' after if.");
            
            var thenjumpLabel = compiler.GotoIfUniqueChunkLabel("if");
            compiler.EmitOpCode(OpCode.POP);

            compiler.Statement();

            var elseJump = compiler.GotoUniqueChunkLabel("else");

            compiler.EmitLabel(thenjumpLabel);
            compiler.EmitOpCode(OpCode.POP);

            if (compiler.TokenIterator.Match(TokenType.ELSE)) compiler.Statement();

            compiler.EmitLabel(elseJump);
        }

        public static void MatchStatement(Compiler compiler)
        {
            //make a scope
            compiler.BeginScope();

            compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect identifier after match statement.");
            var matchArgName = compiler.TokenIterator.PreviousToken.Lexeme;
            var (matchGetOp, _, matchArgID) = compiler.ResolveNameLookupOpCode(matchArgName);

            var lastElseJumpLoc = -1;
            
            var matchEndLabelID = compiler.UniqueChunkStringConstant(nameof(MatchStatement));
            
            compiler.TokenIterator.Consume(TokenType.OPEN_BRACE, "Expect '{' after match expression.");
            do
            {
                if (lastElseJumpLoc != -1)
                    compiler.PatchJump(lastElseJumpLoc);

                compiler.Expression();
                compiler.EmitOpAndBytes(matchGetOp, matchArgID);
                compiler.EmitOpCode(OpCode.EQUAL);
                lastElseJumpLoc = compiler.EmitJumpIf();
                compiler.TokenIterator.Consume(TokenType.COLON, "Expect ':' after match case expression.");
                compiler.Statement();
                compiler.EmitGoto(matchEndLabelID);
            } while (!compiler.TokenIterator.Match(TokenType.CLOSE_BRACE));

            if (lastElseJumpLoc != -1)
                compiler.PatchJump(lastElseJumpLoc);

            compiler.AddConstantAndWriteOp(Value.New($"Match on '{matchArgName}' did have a matching case."));
            compiler.EmitOpCode(OpCode.THROW);

            compiler.EmitLabel(matchEndLabelID);

            compiler.EndScope();
        }

        public static void LabelStatement(Compiler compiler)
        {
            compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect identifier after 'label' statement.");
            var labelName = compiler.TokenIterator.PreviousToken.Lexeme;
            var id = compiler.AddCustomStringConstant(labelName);
            compiler.EmitLabel(id);

            compiler.ConsumeEndStatement();
        }

        public static void GotoStatement(Compiler compiler)
        {
            compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect identifier after 'goto' statement.");
            var labelNameID = compiler.AddStringConstant();

            compiler.EmitGoto(labelNameID);

            compiler.ConsumeEndStatement();
        }

        public static void BreakStatement(Compiler compiler)
        {
            var comp = compiler.CurrentCompilerState;
            if (comp.LoopStates.Count == 0)
                compiler.ThrowCompilerException($"Cannot break when not inside a loop.");

            compiler.EmitNULL();
            compiler.EmitGoto(comp.LoopStates.Peek().ExitLabelID);
            comp.LoopStates.Peek().HasExit = true;

            compiler.ConsumeEndStatement();
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
            InnerFunctionDeclaration(compiler, true);
        }

        public static void Inject(Compiler compiler, bool canAssign)
        {
            compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect property name after 'inject'.");
            byte name = compiler.AddStringConstant();
            compiler.EmitOpAndBytes(OpCode.INJECT, name);
        }

        public static void RegisterStatement(Compiler compiler)
        {
            compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Must provide name after a 'register' statement.");
            var stringConst = compiler.AddStringConstant();
            compiler.Expression();
            compiler.EmitOpAndBytes(OpCode.REGISTER, stringConst);
            compiler.ConsumeEndStatement();
        }

        private static void InnerFunctionDeclaration(Compiler compiler, bool requirePop)
        {
            var functionType = FunctionType.Function;

            if (compiler.TokenIterator.Match(TokenType.PURE))
            {
                functionType = FunctionType.PureFunction;
            }
            if (compiler.TokenIterator.Match(TokenType.LOCAL))
            {
                functionType = FunctionType.LocalFunction;
            }

            var isNamed = compiler.TokenIterator.Check(TokenType.IDENTIFIER);
            var globalName = -1;
            if (isNamed)
            {
                globalName = compiler.ParseVariable("Expect function name.");
                compiler.CurrentCompilerState.MarkInitialised();
            }

            compiler.Function(
                globalName != -1
                ? compiler.TokenIterator.PreviousToken.Lexeme
                : "anonymous",
                functionType);

            if (globalName != -1)
            {
                compiler.DefineVariable((byte)globalName);

                if (!requirePop)
                {
                    var (getOp, _, argId) = compiler.ResolveNameLookupOpCode(compiler.CurrentChunk.ReadConstant((byte)globalName).val.asString.String);
                    compiler.EmitOpAndBytes(getOp, argId);
                }
            }
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
            case TokenType.NULL: compiler.EmitNULL(); break;
            case TokenType.NUMBER:
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
            var endJumpLabel = compiler.GotoIfUniqueChunkLabel("and");

            compiler.EmitOpCode(OpCode.POP);
            compiler.ParsePrecedence(Precedence.And);

            compiler.EmitLabel(endJumpLabel);
        }

        public static void Or(Compiler compiler, bool canAssign)
        {
            var elseJumpLabel = compiler.GotoIfUniqueChunkLabel("else_or");
            var endJump = compiler.GotoUniqueChunkLabel("or");

            compiler.EmitLabel(elseJumpLabel);
            compiler.EmitOpCode(OpCode.POP);

            compiler.ParsePrecedence(Precedence.Or);

            compiler.EmitLabel(endJump);
        }

        public static void Grouping(Compiler compiler, bool canAssign)
        {
            compiler.ExpressionList(TokenType.CLOSE_PAREN, "Expect ')' after expression.");
        }

        public static void FunExp(Compiler compiler, bool canAssign)
        {
            InnerFunctionDeclaration(compiler, false);
        }

        public static void CountOf(Compiler compiler, bool canAssign)
        {
            compiler.Expression();
            compiler.EmitOpCode(OpCode.COUNT_OF);
        }

        public static void Call(Compiler compiler, bool canAssign)
        {
            var argCount = compiler.ArgumentList();
            compiler.EmitOpAndBytes(OpCode.CALL, argCount);
        }

        public static void Meets(Compiler compiler, bool canAssign)
        {
            compiler.Expression();
            compiler.EmitOpCode(OpCode.MEETS);
        }

        public static void Signs(Compiler compiler, bool canAssign)
        {
            compiler.Expression();
            compiler.EmitOpCode(OpCode.SIGNS);
        }

        internal byte GotoUniqueChunkLabel(string v)
        {
            byte labelNameID = UniqueChunkStringConstant(v);
            EmitGoto(labelNameID);
            return labelNameID;
        }

        internal void EmitGoto(byte labelNameID)
        {
            EmitOpAndBytes(OpCode.GOTO, labelNameID);
        }
        
        internal byte GotoIfUniqueChunkLabel(string v)
        {
            byte labelNameID = UniqueChunkStringConstant(v);
            EmitGotoIf(labelNameID);
            return labelNameID;
        }
        
        internal void EmitGotoIf(byte labelNameID)
        {
            EmitOpAndBytes(OpCode.GOTO_IF_FALSE, labelNameID);
        }

        internal byte UniqueChunkStringConstant(string v)
        {
            return AddCustomStringConstant($"{v}_{CurrentChunk.Labels.Count}");
        }

        public byte LabelUniqueChunkLabel(string v)
        {
            byte labelNameID = UniqueChunkStringConstant(v);
            EmitLabel(labelNameID);
            return labelNameID;
        }

        public void EmitLabel(byte id)
        {
            CurrentCompilerState.chunk.AddLabel(id, CurrentChunkInstructinCount);
            EmitOpAndBytes(OpCode.LABEL, id);
        }
    }
}
