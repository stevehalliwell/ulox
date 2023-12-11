﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class Compiler
    {
        private readonly IndexableStack<CompilerState> compilerStates = new IndexableStack<CompilerState>();
        private readonly PrattParserRuleSet _prattParser = new PrattParserRuleSet();
        private readonly TypeInfo _typeInfo = new TypeInfo();

        public TypeInfo TypeInfo => _typeInfo;
        public TokenIterator TokenIterator { get; private set; }

        public TokenType CurrentTokenType
            => TokenIterator?.CurrentToken.TokenType ?? TokenType.NONE;

        public TokenType PreviousTokenType
            => TokenIterator?.PreviousToken.TokenType ?? TokenType.NONE;

        private readonly Dictionary<TokenType, ICompilette> declarationCompilettes = new Dictionary<TokenType, ICompilette>();
        private readonly Dictionary<TokenType, ICompilette> statementCompilettes = new Dictionary<TokenType, ICompilette>();
        private readonly List<Chunk> _allChunks = new List<Chunk>();

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
            var _testdec = new TestSetDeclarationCompilette();
            var _classCompiler = new ClassTypeCompilette();
            var _testcaseCompilette = new TestcaseCompillette(_testdec);

            this.AddDeclarationCompilette(
                new VarDeclarationCompilette(),
                _testdec,
                _classCompiler,
                _testcaseCompilette,
                new BuildCompilette(),
                new EnumTypeCompliette()
                );


            AddDeclarationCompilette(new CompiletteAction(TokenType.FUNCTION, FunctionDeclaration));

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
                (TokenType.FREEZE, FreezeStatement),
                (TokenType.EXPECT, ExpectStatement),
                (TokenType.MATCH, MatchStatement),
                (TokenType.LABEL, LabelStatement),
                (TokenType.GOTO, GotoStatement),
                (TokenType.READ_ONLY, ReadOnlyStatement));

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
                (TokenType.CONTEXT_NAME_TEST, new ActionParseRule(_testcaseCompilette.TestName, null, Precedence.None)),
                (TokenType.CONTEXT_NAME_TESTSET, new ActionParseRule(_testdec.TestSetName, null, Precedence.None)),
                (TokenType.TYPEOF, new ActionParseRule(TypeOf, null, Precedence.Term)),
                (TokenType.MEETS, new ActionParseRule(null, Meets, Precedence.Comparison)),
                (TokenType.SIGNS, new ActionParseRule(null, Signs, Precedence.Comparison)),
                (TokenType.FUNCTION, new ActionParseRule(FunExp, null, Precedence.Call)),
                (TokenType.COUNT_OF, new ActionParseRule(CountOf, null, Precedence.None)),
                (TokenType.UPDATE, new ActionParseRule(null, Update, Precedence.Comparison))
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
            => declarationCompilettes[compilette.MatchingToken] = compilette;

        public void AddStatementCompilette(ICompilette compilette)
            => statementCompilettes[compilette.MatchingToken] = compilette;

        public void SetPrattRule(TokenType tt, IParseRule rule)
            => _prattParser.SetPrattRule(tt, rule);

        public void EmitPacket(ByteCodePacket packet)
            => CurrentChunk.WritePacket(packet, TokenIterator.PreviousToken.Line);

        public void EmitNULL()
            => EmitPacket(new ByteCodePacket(new ByteCodePacket.PushValueDetails(PushValueOpType.Null)));

        public byte AddStringConstant()
            => AddCustomStringConstant((string)TokenIterator.PreviousToken.Literal);

        public void AddConstantAndWriteOp(Value value)
            => CurrentChunk.AddConstantAndWriteInstruction(value, TokenIterator.PreviousToken.Line);

        public byte AddCustomStringConstant(string str)
            => CurrentChunk.AddConstant(Value.New(str));

        public void WriteAt(int at, ByteCodePacket packet)
        {
            CurrentChunk.Instructions[at] = packet;
        }

        public void EndScope()
        {
            var comp = CurrentCompilerState;

            comp.scopeDepth--;

            // We want to group these, so we count them and then write them all at once
            //  the alt is to always combine trailing pops at the write stage but that's more 
            //  of a pure optimisation, as it breaks the line numbering.
            var popCount = default(byte);

            while (comp.localCount > 0 &&
                comp.locals[comp.localCount - 1].Depth > comp.scopeDepth)
            {
                if (comp.locals[comp.localCount - 1].IsCaptured)
                {
                    if (popCount > 0)
                    {
                        EmitPop(popCount);
                        popCount = 0;
                    }

                    EmitPacket(new ByteCodePacket(OpCode.CLOSE_UPVALUE));
                }
                else
                {
                    popCount++;
                }
                CurrentCompilerState.localCount--;
            }

            if (popCount > 0)
            {
                EmitPop(popCount);
            }
        }

        private void PopBackToScopeDepth(int depth)
        {
            var popCount = default(byte);

            var comp = CurrentCompilerState;
            for (int i = comp.localCount - 1; i >= 0; i--)
            {
                if (comp.locals[i].Depth <= depth)
                    break;

                if (!comp.locals[i].IsCaptured)
                    popCount++;
            }

            if (popCount > 0)
                EmitPop(popCount);
        }

        public CompiledScript Compile(Scanner scanner, Script script)
        {
            var tokens = scanner.Scan(script);
            TokenIterator = new TokenIterator(scanner, script, tokens);
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
            EmitPop();
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
                chunk = new Chunk(name, TokenIterator?.SourceName),
            };
            compilerStates.Push(newCompState);
            CurrentCompilerState.AddLocal(this, "", 0);
        }

        public void NamedVariable(string name, bool canAssign)
        {
            (var getOp, var setOp, var argId) = ResolveNameLookupOpCode(name);

            if (!canAssign)
            {
                EmitPacket(new ByteCodePacket(getOp, argId));
                return;
            }

            if (TokenIterator.Match(TokenType.ASSIGN))
            {
                Expression();

                EmitPacket(new ByteCodePacket(setOp, argId));
                return;
            }

            if (HandleCompoundAssignToken(getOp, setOp, argId))
            {
                return;
            }

            EmitPacket(new ByteCodePacket(getOp, argId));
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
                EmitPacket(new ByteCodePacket(getOp, argId));
                EmitPacket(new ByteCodePacket(OpCode.SWAP));

                // self assign ops have to be done here as they tail the previous ordered instructions
                switch (assignTokenType)
                {
                case TokenType.PLUS_EQUAL:
                    EmitPacket(new ByteCodePacket(OpCode.ADD));
                    break;

                case TokenType.MINUS_EQUAL:
                    EmitPacket(new ByteCodePacket(OpCode.SUBTRACT));
                    break;

                case TokenType.STAR_EQUAL:
                    EmitPacket(new ByteCodePacket(OpCode.MULTIPLY));
                    break;

                case TokenType.SLASH_EQUAL:
                    EmitPacket(new ByteCodePacket(OpCode.DIVIDE));
                    break;

                case TokenType.PERCENT_EQUAL:
                    EmitPacket(new ByteCodePacket(OpCode.MODULUS));
                    break;
                }

                EmitPacket(new ByteCodePacket(setOp, argId));
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

        public Chunk EndCompile()
        {
            EmitReturn();
            var returnChunk = compilerStates.Pop().chunk;
            _allChunks.Add(returnChunk);
            return returnChunk;
        }

        public void EmitReturn()
        {
            EmitPacket(new ByteCodePacket(OpCode.RETURN));
        }

        private void PreEmptyReturnEmit()
        {
            if (CurrentCompilerState.functionType == FunctionType.Init)
                EmitPacket(new ByteCodePacket(OpCode.GET_LOCAL, (byte)0));
            else
                EmitNULL();
        }

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

        public Chunk Function(string name, FunctionType functionType)
        {
            if (functionType == FunctionType.Method
               || functionType == FunctionType.Init)
            {
                ThrowCompilerException($"Cannot declare a {functionType} function outside of a class.");
            }

            PushCompilerState(name, functionType);

            BeginScope();
            VariableNameListDeclareOptional(() => IncreaseArity(AddStringConstant()));
            var returnCount = VariableNameListDeclareOptional(() => IncreaseReturn(AddStringConstant()));

            if (returnCount == 0)
            {
                var retvalId = DeclareAndDefineCustomVariable("retval");
                IncreaseReturn(retvalId);
            }

            // The body.
            TokenIterator.Consume(TokenType.OPEN_BRACE, "Expect '{' before function body.");
            Block();

            // Create the function object.
            var comp = CurrentCompilerState;   //we need this to mark upvalues
            var function = EndCompile();
            EmitPacket(new ByteCodePacket(OpCode.CLOSURE, new ByteCodePacket.ClosureDetails(ClosureType.Closure, CurrentChunk.AddConstant(Value.New(function)), (byte)function.UpvalueCount)));

            for (int i = 0; i < function.UpvalueCount; i++)
            {
                EmitPacket(
                    new ByteCodePacket(OpCode.CLOSURE, new ByteCodePacket.ClosureDetails(
                    ClosureType.UpValueInfo,
                    comp.upvalues[i].isLocal ? (byte)1 : (byte)0,
                    comp.upvalues[i].index)));
            }

            return function;
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

        public void IncreaseReturn(byte argNameConstant)
        {
            CurrentChunk.ReturnConstantIds.Add(argNameConstant);
            if (CurrentChunk.ReturnCount > 255)
                ThrowCompilerException($"Can't have more than 255 returns.");
        }

        public void Block()
        {
            while (!TokenIterator.Check(TokenType.CLOSE_BRACE)
                && !TokenIterator.Check(TokenType.EOF))
            {
                Declaration();
            }

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

            EmitPacket(new ByteCodePacket(OpCode.DEFINE_GLOBAL, global, 0, 0));
        }

        public byte DeclareAndDefineCustomVariable(string varName)
        {
            //do equiv of ParseVariable, DefineVariable
            CurrentCompilerState.DeclareVariableByName(this, varName);
            var id = AddCustomStringConstant(varName);
            DefineVariable(id);
            return id;
        }

        public byte DeclareAndDefineLocal(string itemName, string errorPrefix)
        {
            if (CurrentCompilerState.ResolveLocal(this, itemName) != -1)
                ThrowCompilerException($"{errorPrefix} '{itemName}' already exists at this scope");

            CurrentCompilerState.DeclareVariableByName(this, itemName);
            CurrentCompilerState.MarkInitialised();
            var itemArgId = (byte)CurrentCompilerState.ResolveLocal(this, itemName);
            EmitPacket(new ByteCodePacket(new ByteCodePacket.PushValueDetails(0)));
            EmitPacket(new ByteCodePacket(OpCode.SET_LOCAL, itemArgId));
            return itemArgId;
        }

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
            case TokenType.PLUS: compiler.EmitPacket(new ByteCodePacket(OpCode.ADD)); break;
            case TokenType.MINUS: compiler.EmitPacket(new ByteCodePacket(OpCode.SUBTRACT)); break;
            case TokenType.STAR: compiler.EmitPacket(new ByteCodePacket(OpCode.MULTIPLY)); break;
            case TokenType.SLASH: compiler.EmitPacket(new ByteCodePacket(OpCode.DIVIDE)); break;
            case TokenType.PERCENT: compiler.EmitPacket(new ByteCodePacket(OpCode.MODULUS)); break;
            case TokenType.EQUALITY: compiler.EmitPacket(new ByteCodePacket(OpCode.EQUAL)); break;
            case TokenType.GREATER: compiler.EmitPacket(new ByteCodePacket(OpCode.GREATER)); break;
            case TokenType.LESS: compiler.EmitPacket(new ByteCodePacket(OpCode.LESS)); break;
            case TokenType.BANG_EQUAL: compiler.EmitPacket(new ByteCodePacket(OpCode.EQUAL)); compiler.EmitPacket(new ByteCodePacket(OpCode.NOT)); break;
            case TokenType.GREATER_EQUAL: compiler.EmitPacket(new ByteCodePacket(OpCode.LESS)); compiler.EmitPacket(new ByteCodePacket(OpCode.NOT)); break;
            case TokenType.LESS_EQUAL: compiler.EmitPacket(new ByteCodePacket(OpCode.GREATER)); compiler.EmitPacket(new ByteCodePacket(OpCode.NOT)); break;

            default:
                break;
            }
        }

        public static void FreezeStatement(Compiler compiler)
        {
            compiler.Expression();
            compiler.EmitPacket(new ByteCodePacket(OpCode.FREEZE));
            compiler.ConsumeEndStatement();
        }

        public static void ExpectStatement(Compiler compiler)
        {
            do
            {
                //find start of the string so we can later substr it if desired
                var startIndex = compiler.TokenIterator.PreviousToken.StringSourceIndex + 1;
                compiler.Expression();
                if (compiler.TokenIterator.Match(TokenType.COLON))
                {
                    compiler.Expression();
                }
                else
                {
                    var endIndex = compiler.TokenIterator.CurrentToken.StringSourceIndex;
                    var length = endIndex - startIndex;
                    var sourceStringSection = compiler.TokenIterator.GetSourceSection(startIndex, length);
                    var sectionByte = compiler.AddCustomStringConstant(sourceStringSection.Trim());
                    compiler.EmitPacket(new ByteCodePacket(OpCode.PUSH_CONSTANT, sectionByte, 0, 0));
                }
                compiler.EmitPacket(new ByteCodePacket(OpCode.EXPECT));
            }
            while (compiler.TokenIterator.Match(TokenType.COMMA));

            compiler.ConsumeEndStatement();
        }

        private static void BraceCreateDynamic(Compiler compiler, bool arg2)
        {
            var midTok = TokenType.ASSIGN;
            if (compiler.TokenIterator.Match(midTok)
                  && compiler.TokenIterator.Match(TokenType.CLOSE_BRACE))
            {
                compiler.EmitPacket(new ByteCodePacket(OpCode.NATIVE_TYPE, NativeType.Dynamic));
            }
            else if (compiler.TokenIterator.Check(TokenType.IDENTIFIER))
            {
                compiler.EmitPacket(new ByteCodePacket(OpCode.NATIVE_TYPE, NativeType.Dynamic));

                while (!compiler.TokenIterator.Match(TokenType.CLOSE_BRACE))
                {
                    //we need to copy the dynamic inst
                    compiler.EmitPacket(new ByteCodePacket(OpCode.DUPLICATE));
                    compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect identifier.");
                    //add the constant
                    var identConstantID = compiler.AddStringConstant();
                    //read the colon
                    compiler.TokenIterator.Consume(midTok, "Expect '=' after identifiier.");
                    //do expression
                    compiler.Expression();
                    //we need a set property
                    compiler.EmitPacket(new ByteCodePacket(OpCode.SET_PROPERTY, identConstantID));
                    compiler.EmitPop();

                    //if comma consume
                    compiler.TokenIterator.Match(TokenType.COMMA);
                }
            }
            else
            {
                compiler.ThrowCompilerException("Expect identifier or '=' after '{'");
            }
        }

        public static void TypeOf(Compiler compiler, bool canAssign)
        {
            compiler.TokenIterator.Consume(TokenType.OPEN_PAREN, "Expect '(' after typeof.");
            compiler.Expression();
            compiler.TokenIterator.Consume(TokenType.CLOSE_PAREN, "Expect ')' after typeof.");
            compiler.EmitPacket(new ByteCodePacket(OpCode.TYPEOF));
        }

        public static void BracketCreate(Compiler compiler, bool canAssign)
        {
            if (compiler.TokenIterator.Match(TokenType.COLON)
                && compiler.TokenIterator.Match(TokenType.CLOSE_BRACKET))
            {
                compiler.EmitPacket(new ByteCodePacket(OpCode.NATIVE_TYPE, NativeType.Map));
                return;
            }

            var nativeTypeInstruction = compiler.CurrentChunkInstructinCount;
            compiler.EmitPacket(new ByteCodePacket(OpCode.NATIVE_TYPE, NativeType.List));

            var firstLoop = true;
            var isList = true;

            while (!compiler.TokenIterator.Check(TokenType.CLOSE_BRACKET))
            {
                compiler.EmitPacket(new ByteCodePacket(OpCode.DUPLICATE));
                compiler.Expression();

                if (firstLoop
                    && compiler.TokenIterator.Check(TokenType.COLON))
                {
                    //switch to map
                    isList = false;
                    compiler.WriteAt(nativeTypeInstruction, new ByteCodePacket(OpCode.NATIVE_TYPE, NativeType.Map));
                }

                if (isList)
                {
                    var constantNameId = compiler.AddCustomStringConstant("Add");
                    const byte argCount = 1;
                    compiler.EmitPacket(new ByteCodePacket(OpCode.INVOKE, constantNameId, argCount, 0));
                }
                else
                {
                    compiler.TokenIterator.Consume(TokenType.COLON, "Expect ':' after key");
                    compiler.Expression();
                    compiler.EmitPacket(new ByteCodePacket(OpCode.SET_INDEX));
                }
                compiler.EmitPop();

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
                compiler.EmitPacket(new ByteCodePacket(OpCode.SET_INDEX));
            }
            else
            {
                compiler.EmitPacket(new ByteCodePacket(OpCode.GET_INDEX));
            }
        }

        public static void Dot(Compiler compiler, bool canAssign)
        {
            compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect property name after '.'.");
            byte nameId = compiler.AddStringConstant();

            if (canAssign && compiler.TokenIterator.Match(TokenType.ASSIGN))
            {
                compiler.Expression();
                compiler.EmitPacket(new ByteCodePacket(OpCode.SET_PROPERTY, nameId));
            }
            else if (compiler.TokenIterator.Match(TokenType.OPEN_PAREN))
            {
                var argCount = compiler.ArgumentList();
                compiler.EmitPacket(new ByteCodePacket(OpCode.INVOKE, nameId, argCount));
            }
            else
            {
                compiler.EmitPacket(new ByteCodePacket(OpCode.GET_PROPERTY, nameId));
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
            compiler.EmitPacket(new ByteCodePacket(OpCode.THROW));
        }

        public static void ContinueStatement(Compiler compiler)
        {
            var comp = compiler.CurrentCompilerState;
            if (comp.LoopStates.Count == 0)
                compiler.ThrowCompilerException($"Cannot continue when not inside a loop.");

            compiler.PopBackToScopeDepth(comp.LoopStates.Last().ScopeDepth);
            compiler.EmitGoto(comp.LoopStates.Peek().ContinueLabelID);
            compiler.ConsumeEndStatement();
        }

        public static void BreakStatement(Compiler compiler)
        {
            var comp = compiler.CurrentCompilerState;
            if (comp.LoopStates.Count == 0)
                compiler.ThrowCompilerException($"Cannot break when not inside a loop.");


            compiler.PopBackToScopeDepth(comp.LoopStates.Last().ScopeDepth);

            compiler.EmitNULL();
            compiler.EmitGoto(comp.LoopStates.Peek().ExitLabelID);
            comp.LoopStates.Peek().HasExit = true;

            compiler.ConsumeEndStatement();
        }

        public static void IfStatement(Compiler compiler)
        {
            compiler.TokenIterator.Consume(TokenType.OPEN_PAREN, "Expect '(' after if.");
            compiler.Expression();
            compiler.TokenIterator.Consume(TokenType.CLOSE_PAREN, "Expect ')' after if.");

            //todo can we make goto_if consume the value in the vm so we don't need to play pop wackamole
            var thenjumpLabel = compiler.GotoIfUniqueChunkLabel("if_false");
            compiler.EmitPop();

            compiler.Statement();

            var afterIfLabel = compiler.GotoUniqueChunkLabel("if_end");

            if (compiler.TokenIterator.Match(TokenType.ELSE))
            {
                var elseJump = compiler.GotoUniqueChunkLabel("else");

                compiler.EmitLabel(thenjumpLabel);
                compiler.EmitPop();

                compiler.Statement();

                compiler.EmitLabel(elseJump);
            }
            else
            {
                compiler.EmitLabel(thenjumpLabel);
                compiler.EmitPop();
            }

            compiler.EmitLabel(afterIfLabel);
        }

        public static void MatchStatement(Compiler compiler)
        {
            //make a scope
            compiler.BeginScope();

            compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect identifier after match statement.");
            var matchArgName = compiler.TokenIterator.PreviousToken.Lexeme;
            var (matchGetOp, _, matchArgID) = compiler.ResolveNameLookupOpCode(matchArgName);

            var lastElseLabel = -1;

            var matchEndLabelID = compiler.UniqueChunkLabelStringConstant(nameof(MatchStatement));

            compiler.TokenIterator.Consume(TokenType.OPEN_BRACE, "Expect '{' after match expression.");
            do
            {
                if (lastElseLabel != -1)
                {
                    compiler.EmitLabel((byte)lastElseLabel);
                    compiler.EmitPop();
                }

                compiler.Expression();
                compiler.EmitPacket(new ByteCodePacket(matchGetOp, matchArgID));
                compiler.EmitPacket(new ByteCodePacket(OpCode.EQUAL));
                lastElseLabel = compiler.GotoIfUniqueChunkLabel("match");
                compiler.EmitPop();
                compiler.TokenIterator.Consume(TokenType.COLON, "Expect ':' after match case expression.");
                compiler.Statement();
                compiler.EmitGoto(matchEndLabelID);
            } while (!compiler.TokenIterator.Match(TokenType.CLOSE_BRACE));

            if (lastElseLabel != -1)
                compiler.EmitLabel((byte)lastElseLabel);

            compiler.AddConstantAndWriteOp(Value.New($"Match on '{matchArgName}' did have a matching case."));
            compiler.EmitPacket(new ByteCodePacket(OpCode.THROW));

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

        public static void ReadOnlyStatement(Compiler compiler)
        {
            compiler.Expression();
            compiler.EmitPacket(new ByteCodePacket(OpCode.READ_ONLY));

            compiler.ConsumeEndStatement();
        }

        public static void YieldStatement(Compiler compiler)
        {
            compiler.EmitPacket(new ByteCodePacket(OpCode.YIELD));

            compiler.ConsumeEndStatement();
        }

        public static void BlockStatement(Compiler compiler)
            => compiler.BlockStatement();

        public static void FunctionDeclaration(Compiler compiler)
        {
            InnerFunctionDeclaration(compiler, true);
        }

        private static void InnerFunctionDeclaration(Compiler compiler, bool requirePop)
        {
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
                 FunctionType.Function);

            if (globalName != -1)
            {
                compiler.DefineVariable((byte)globalName);

                if (!requirePop)
                {
                    var (getOp, _, argId) = compiler.ResolveNameLookupOpCode(compiler.CurrentChunk.ReadConstant((byte)globalName).val.asString.String);
                    compiler.EmitPacket(new ByteCodePacket(getOp, argId));
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
            case TokenType.MINUS: compiler.EmitPacket(new ByteCodePacket(OpCode.NEGATE)); break;
            case TokenType.BANG: compiler.EmitPacket(new ByteCodePacket(OpCode.NOT)); break;
            default:
                break;
            }
        }

        public static void Literal(Compiler compiler, bool canAssign)
        {
            switch (compiler.PreviousTokenType)
            {
            case TokenType.TRUE: compiler.EmitPacket(new ByteCodePacket(new ByteCodePacket.PushValueDetails(true))); break;
            case TokenType.FALSE: compiler.EmitPacket(new ByteCodePacket(new ByteCodePacket.PushValueDetails(false))); break;
            case TokenType.NULL: compiler.EmitNULL(); break;
            case TokenType.NUMBER:
            {
                var number = (double)compiler.TokenIterator.PreviousToken.Literal;

                compiler.DoNumberConstant(number);
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

        public void DoNumberConstant(double number)
        {
            var isInt = number == Math.Truncate(number);

            if (isInt && number < int.MaxValue && number >= int.MinValue)
            {
                EmitPacket(new ByteCodePacket(new ByteCodePacket.PushValueDetails((int)number)));
                return;
            }

            var asFloat = (float)number;
            var asDoubleAgain = (double)asFloat;
            var convertedDif = Math.Abs(number - asDoubleAgain);
            var relativeDif = convertedDif / number;
            var isFloat = !float.IsNaN(asFloat)
                && !double.IsNaN(convertedDif)
                && relativeDif < 0.00001;

            if (isFloat)
            {
                EmitPacket(new ByteCodePacket(new ByteCodePacket.PushValueDetails(asFloat)));
                return;
            }

            AddConstantAndWriteOp(Value.New(number));
        }

        public static void Variable(Compiler compiler, bool canAssign)
        {
            var name = (string)compiler.TokenIterator.PreviousToken.Literal;
            compiler.NamedVariable(name, canAssign);
        }

        public static void And(Compiler compiler, bool canAssign)
        {
            var endJumpLabel = compiler.GotoIfUniqueChunkLabel("and");

            compiler.EmitPop();
            compiler.ParsePrecedence(Precedence.And);

            compiler.EmitLabel(endJumpLabel);
        }

        public static void Or(Compiler compiler, bool canAssign)
        {
            var elseJumpLabel = compiler.GotoIfUniqueChunkLabel("else_or");
            var endJump = compiler.GotoUniqueChunkLabel("or");

            compiler.EmitLabel(elseJumpLabel);
            compiler.EmitPop();

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
            compiler.EmitPacket(new ByteCodePacket(OpCode.COUNT_OF));
        }

        public static void Update(Compiler compiler, bool canAssign)
        {
            compiler.Expression();
            compiler.EmitPacket(new ByteCodePacket(OpCode.UPDATE));
        }

        public static void Call(Compiler compiler, bool canAssign)
        {
            var argCount = compiler.ArgumentList();
            compiler.EmitPacket(new ByteCodePacket(OpCode.CALL, argCount, 0, 0));
        }

        public static void Meets(Compiler compiler, bool canAssign)
        {
            compiler.Expression();
            compiler.EmitPacket(new ByteCodePacket(OpCode.VALIDATE, ValidateOp.Meets));
        }

        public static void Signs(Compiler compiler, bool canAssign)
        {
            compiler.Expression();
            compiler.EmitPacket(new ByteCodePacket(OpCode.VALIDATE, ValidateOp.Signs));
        }

        internal byte GotoUniqueChunkLabel(string v)
        {
            byte labelNameID = UniqueChunkLabelStringConstant(v);
            EmitGoto(labelNameID);
            return labelNameID;
        }

        internal void EmitGoto(byte labelNameID)
        {
            EmitPacket(new ByteCodePacket(OpCode.GOTO, labelNameID, 0, 0));
        }

        internal byte GotoIfUniqueChunkLabel(string v)
        {
            byte labelNameID = UniqueChunkLabelStringConstant(v);
            EmitGotoIf(labelNameID);
            return labelNameID;
        }

        internal void EmitGotoIf(byte labelNameID)
        {
            EmitPacket(new ByteCodePacket(OpCode.GOTO_IF_FALSE, labelNameID, 0, 0));
        }

        internal byte UniqueChunkLabelStringConstant(string v)
        {
            var id = AddCustomStringConstant($"{v}_{CurrentChunk.Labels.Count}");
            CurrentCompilerState.chunk.AddLabel(id, -1);
            return id;
        }

        public byte LabelUniqueChunkLabel(string v)
        {
            byte labelNameID = UniqueChunkLabelStringConstant(v);
            EmitLabel(labelNameID);
            return labelNameID;
        }

        public void EmitLabel(byte id)
        {
            CurrentCompilerState.chunk.AddLabel(id, CurrentChunkInstructinCount);
            EmitPacket(new ByteCodePacket(OpCode.LABEL, id, 0, 0));
        }

        internal void EmitPop(byte popCount = 1)
        {
            EmitPacket(new ByteCodePacket(OpCode.POP, popCount));
        }

        internal string IdentifierOrChunkUnique(string prefix)
        {
            if (TokenIterator.Match(TokenType.IDENTIFIER))
                return TokenIterator.PreviousToken.Lexeme;

            var existingPrefixes = CurrentChunk.Constants.Count(x => x.type == ValueType.String && x.val.asString.String.Contains(prefix));
            return $"{prefix}{CurrentChunk.SourceName}{existingPrefixes}";
        }
    }
}
