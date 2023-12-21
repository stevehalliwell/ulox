using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ULox
{
    public sealed class Compiler
    {
        private readonly IndexableStack<CompilerState> compilerStates = new IndexableStack<CompilerState>();
        //temp
        public readonly PrattParserRuleSet _prattParser = new PrattParserRuleSet();
        private readonly TypeInfo _typeInfo = new TypeInfo();

        public TypeInfo TypeInfo => _typeInfo;
        public TokenIterator TokenIterator { get; private set; }

        private readonly Dictionary<TokenType, ICompilette> declarationCompilettes = new Dictionary<TokenType, ICompilette>();
        private readonly Dictionary<TokenType, ICompilette> statementCompilettes = new Dictionary<TokenType, ICompilette>();
        private readonly List<Chunk> _allChunks = new List<Chunk>();
        private readonly ClassTypeCompilette _classCompiler = new ClassTypeCompilette();

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
            var _testcaseCompilette = new TestcaseCompillette(_testdec);

            AddDeclarationCompilette(
                _testdec,
                _classCompiler,
                _testcaseCompilette,
                new EnumTypeDeclarationCompliette()
                );

            AddDeclarationCompilette(
                (TokenType.FUNCTION, CompilerDeclarations.FunctionDeclaration),
                (TokenType.VAR, CompilerDeclarations.VarDeclaration)
                );

            AddStatementCompilette(
                (TokenType.IF, CompilerStatements.IfStatement),
                (TokenType.YIELD, CompilerStatements.YieldStatement),
                (TokenType.BREAK, CompilerStatements.BreakStatement),
                (TokenType.CONTINUE, CompilerStatements.ContinueStatement),
                (TokenType.OPEN_BRACE, CompilerStatements.BlockStatement),
                (TokenType.THROW, CompilerStatements.ThrowStatement),
                (TokenType.END_STATEMENT, CompilerStatements.NoOpStatement),
                (TokenType.FREEZE, CompilerStatements.FreezeStatement),
                (TokenType.EXPECT, CompilerStatements.ExpectStatement),
                (TokenType.MATCH, CompilerStatements.MatchStatement),
                (TokenType.LABEL, CompilerStatements.LabelStatement),
                (TokenType.GOTO, CompilerStatements.GotoStatement),
                (TokenType.READ_ONLY, CompilerStatements.ReadOnlyStatement),
                (TokenType.RETURN, CompilerStatements.ReturnStatement),
                (TokenType.FOR, CompilerStatements.ForStatement),
                (TokenType.BUILD, CompilerStatements.BuildStatement)
                );

            SetPrattRules(
                (TokenType.MINUS, CompilerExpressions.Unary, CompilerExpressions.Binary, Precedence.Term),
                (TokenType.PLUS, null, CompilerExpressions.Binary, Precedence.Term),
                (TokenType.SLASH, null, CompilerExpressions.Binary, Precedence.Factor),
                (TokenType.STAR, null, CompilerExpressions.Binary, Precedence.Factor),
                (TokenType.PERCENT, null, CompilerExpressions.Binary, Precedence.Factor),
                (TokenType.BANG, CompilerExpressions.Unary, null, Precedence.None),
                (TokenType.NUMBER, CompilerExpressions.Literal, null, Precedence.None),
                (TokenType.TRUE, CompilerExpressions.Literal, null, Precedence.None),
                (TokenType.FALSE, CompilerExpressions.Literal, null, Precedence.None),
                (TokenType.NULL, CompilerExpressions.Literal, null, Precedence.None),
                (TokenType.BANG_EQUAL, null, CompilerExpressions.Binary, Precedence.Equality),
                (TokenType.EQUALITY, null, CompilerExpressions.Binary, Precedence.Equality),
                (TokenType.LESS, null, CompilerExpressions.Binary, Precedence.Comparison),
                (TokenType.LESS_EQUAL, null, CompilerExpressions.Binary, Precedence.Comparison),
                (TokenType.GREATER, null, CompilerExpressions.Binary, Precedence.Comparison),
                (TokenType.GREATER_EQUAL, null, CompilerExpressions.Binary, Precedence.Comparison),
                (TokenType.STRING, CompilerExpressions.Literal, null, Precedence.None),
                (TokenType.IDENTIFIER, CompilerExpressions.Variable, null, Precedence.None),
                (TokenType.AND, null, CompilerExpressions.And, Precedence.And),
                (TokenType.OR, null, CompilerExpressions.Or, Precedence.Or),
                (TokenType.OPEN_PAREN, CompilerExpressions.Grouping, CompilerExpressions.Call, Precedence.Call),
                (TokenType.CONTEXT_NAME_FUNC, CompilerExpressions.FName, null, Precedence.None),
                (TokenType.OPEN_BRACKET, CompilerExpressions.BracketCreate, CompilerExpressions.BracketSubScript, Precedence.Call),
                (TokenType.OPEN_BRACE, CompilerExpressions.BraceCreateDynamic, null, Precedence.Call),
                (TokenType.DOT, null, CompilerExpressions.Dot, Precedence.Call),
                (TokenType.THIS, _classCompiler.This, null, Precedence.None),
                (TokenType.CONTEXT_NAME_CLASS, _classCompiler.CName, null, Precedence.None),
                (TokenType.CONTEXT_NAME_TEST, _testcaseCompilette.TestName, null, Precedence.None),
                (TokenType.CONTEXT_NAME_TESTSET, _testdec.TestSetName, null, Precedence.None),
                (TokenType.TYPEOF, CompilerExpressions.TypeOf, null, Precedence.Term),
                (TokenType.MEETS, null, CompilerExpressions.Meets, Precedence.Comparison),
                (TokenType.SIGNS, null, CompilerExpressions.Signs, Precedence.Comparison),
                (TokenType.FUNCTION, CompilerExpressions.FunExp, null, Precedence.Call),
                (TokenType.COUNT_OF, CompilerExpressions.CountOf, null, Precedence.None),
                (TokenType.UPDATE, null, CompilerExpressions.Update, Precedence.Comparison)
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

        public void AddDeclarationCompilette(params (TokenType match, Action<Compiler> action)[] compilettes)
        {
            foreach (var item in compilettes)
            {
                AddDeclarationCompilette(new CompiletteAction(item.match, item.action));
            }
        }

        public void AddDeclarationCompilette(params ICompilette[] compilettes)
        {
            foreach (var item in compilettes)
            {
                AddDeclarationCompilette(item);
            }
        }

        public void AddStatementCompilette(params (TokenType match, System.Action<Compiler> action)[] processActions)
        {
            foreach (var item in processActions)
            {
                AddStatementCompilette(new CompiletteAction(item.match, item.action));
            }
        }

        public void SetPrattRules(params (TokenType tt, Action<Compiler, bool> prefix, Action<Compiler, bool> infix, Precedence pre)[] rules)
        {
            foreach (var (tt, preAct, inAct, prec) in rules)
            {
                SetPrattRule(tt, new ActionParseRule(preAct, inAct, prec));
            }
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

        public void PopBackToScopeDepth(int depth)
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
            TokenIterator = new TokenIterator(script, tokens);
            TokenIterator.Advance();

            PushCompilerState(string.Empty, FunctionType.Script);

            while (TokenIterator.CurrentToken.TokenType != TokenType.EOF)
            {
                Declaration();
            }

            var topChunk = EndCompile();
            return new CompiledScript(topChunk, script.GetHashCode(), _allChunks.GetRange(0, _allChunks.Count));
        }

        public void Declaration()
        {
            if (declarationCompilettes.TryGetValue(TokenIterator.CurrentToken.TokenType, out var complette))
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
            if (statementCompilettes.TryGetValue(TokenIterator.CurrentToken.TokenType, out var complette))
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
            var resolveRes = ResolveNameLookupOpCode(name);

            if (!canAssign)
            {
                EmitPacketFromResolveGet(resolveRes);
                return;
            }

            if (TokenIterator.Match(TokenType.ASSIGN))
            {
                Expression();

                EmitPacketFromResolveSet(resolveRes);
                return;
            }

            EmitPacketFromResolveGet(resolveRes);
        }

        public void EmitPacketFromResolveGet(ResolveNameLookupResult resolveRes)
        {
            EmitPacket(new ByteCodePacket(resolveRes.GetOp, resolveRes.ArgId));
        }

        public void EmitPacketFromResolveSet(ResolveNameLookupResult resolveRes)
        {
            EmitPacket(new ByteCodePacket(resolveRes.SetOp, resolveRes.ArgId));
        }

        public struct ResolveNameLookupResult
        {
            public OpCode GetOp;
            public OpCode SetOp;
            public byte ArgId;

            public ResolveNameLookupResult(OpCode getOp, OpCode setOp, byte argId)
            {
                GetOp = getOp;
                SetOp = setOp;
                ArgId = argId;
            }
        }

        public ResolveNameLookupResult ResolveNameLookupOpCode(string name)
        {
            var argId = CurrentCompilerState.ResolveLocal(this, name);
            if (argId != -1)
            {
                return new ResolveNameLookupResult(OpCode.GET_LOCAL, OpCode.SET_LOCAL, (byte)argId);
            }

            argId = CurrentCompilerState.ResolveUpvalue(this, name);
            if (argId != -1)
            {
                return new ResolveNameLookupResult(OpCode.GET_UPVALUE, OpCode.SET_UPVALUE, (byte)argId);
            }

            if (!string.IsNullOrEmpty(_classCompiler.CurrentTypeName)
                && _classCompiler.CurrentTypeInfoEntry.Fields.Contains(name))
            {
                return new ResolveNameLookupResult(OpCode.GET_FIELD, OpCode.SET_FIELD, AddCustomStringConstant(name));
            }

            argId = CurrentChunk.AddConstant(Value.New(name));
            return new ResolveNameLookupResult(OpCode.FETCH_GLOBAL, OpCode.ASSIGN_GLOBAL, (byte)argId);
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

        public static void InnerFunctionDeclaration(Compiler compiler, bool requirePop)
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
                    var resolveRes = compiler.ResolveNameLookupOpCode(compiler.CurrentChunk.ReadConstant((byte)globalName).val.asString.String);
                    compiler.EmitPacketFromResolveGet(resolveRes);
                }
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

        internal byte ResolveLocal(string argName)
        {
            return (byte)CurrentCompilerState.ResolveLocal(this, argName);
        }

        public bool DoesLocalAlreadyExist(string argName)
        {
            return CurrentCompilerState.ResolveLocal(this, argName) != -1;
        }
    }
}
