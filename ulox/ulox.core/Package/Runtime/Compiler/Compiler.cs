using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using static ULox.CompilerState;

namespace ULox
{
    public class CompilerException : UloxException
    {
        public CompilerException(string msg)
            : base(msg)
        {
        }
    }

    public enum PushValueOpType : byte
    {
        Null,
        Bool,
        Short,
    }

    public interface ICompilette
    {
        TokenType MatchingToken { get; }

        void Process(Compiler compiler);
    }

    public sealed class Compiler : ICompilerDesugarContext
    {
        //temp
        public readonly PrattParserRuleSet _prattParser = new();
        private readonly TypeInfo _typeInfo = new();

        public TypeInfo TypeInfo => _typeInfo;

        private CompiledScript _compilingScript;

        public TokenIterator TokenIterator { get; private set; }

        private readonly IndexableStack<CompilerState> _compilerStates = new();
        private readonly Dictionary<TokenType, ICompilette> _declarationCompilettes = new();
        private readonly Dictionary<TokenType, ICompilette> _statementCompilettes = new();
        private readonly ClassTypeCompilette _classCompiler = new();

        public int CurrentChunkInstructionCount => CurrentChunk.Instructions.Count;
        public Chunk CurrentChunk => CurrentCompilerState.chunk;
        public CompilerState CurrentCompilerState => _compilerStates.Peek();

        public Compiler()
        {
            Setup();
            Reset();
        }

        private void Setup()
        {
            var testdec = new TestSetDeclarationCompilette();
            var testcaseCompilette = new TestcaseCompillette(testdec);

            AddDeclarationCompilette(
                testdec,
                _classCompiler,
                testcaseCompilette,
                new EnumTypeDeclarationCompliette()
                );

            AddDeclarationCompilette(
                (TokenType.FUNCTION, FunctionDeclaration),
                (TokenType.VAR, VarDeclaration)
                );

            AddStatementCompilette(
                (TokenType.IF, IfStatement),
                (TokenType.YIELD, YieldStatement),
                (TokenType.BREAK, BreakStatement),
                (TokenType.CONTINUE, ContinueStatement),
                (TokenType.OPEN_BRACE, BlockStatement),
                (TokenType.PANIC, PanicStatement),
                (TokenType.END_STATEMENT, NoOpStatement),
                (TokenType.EXPECT, ExpectStatement),
                (TokenType.MATCH, MatchStatement),
                (TokenType.LABEL, LabelStatement),
                (TokenType.GOTO, GotoStatement),
                (TokenType.READ_ONLY, ReadOnlyStatement),
                (TokenType.RETURN, ReturnStatement),
                (TokenType.FOR, ForStatement),
                (TokenType.BUILD, BuildStatement)
                );

            SetPrattRules(
                (TokenType.MINUS, Unary, Binary, Precedence.Term),
                (TokenType.PLUS, null, Binary, Precedence.Term),
                (TokenType.SLASH, null, Binary, Precedence.Factor),
                (TokenType.STAR, null, Binary, Precedence.Factor),
                (TokenType.PERCENT, null, Binary, Precedence.Factor),
                (TokenType.BANG, Unary, null, Precedence.None),
                (TokenType.NUMBER, Literal, null, Precedence.None),
                (TokenType.TRUE, Literal, null, Precedence.None),
                (TokenType.FALSE, Literal, null, Precedence.None),
                (TokenType.NULL, Literal, null, Precedence.None),
                (TokenType.BANG_EQUAL, null, Binary, Precedence.Equality),
                (TokenType.EQUALITY, null, Binary, Precedence.Equality),
                (TokenType.LESS, null, Binary, Precedence.Comparison),
                (TokenType.LESS_EQUAL, null, Binary, Precedence.Comparison),
                (TokenType.GREATER, null, Binary, Precedence.Comparison),
                (TokenType.GREATER_EQUAL, null, Binary, Precedence.Comparison),
                (TokenType.STRING, Literal, null, Precedence.None),
                (TokenType.IDENTIFIER, Variable, null, Precedence.None),
                (TokenType.AND, null, And, Precedence.And),
                (TokenType.OR, null, Or, Precedence.Or),
                (TokenType.OPEN_PAREN, Grouping, Call, Precedence.Call),
                (TokenType.CONTEXT_NAME_FUNC, FName, null, Precedence.None),
                (TokenType.OPEN_BRACKET, BracketCreate, BracketSubScript, Precedence.Call),
                (TokenType.OPEN_BRACE, BraceCreateDynamic, null, Precedence.Call),
                (TokenType.DOT, null, Dot, Precedence.Call),
                (TokenType.THIS, _classCompiler.This, null, Precedence.None),
                (TokenType.CONTEXT_NAME_CLASS, _classCompiler.CName, null, Precedence.None),
                (TokenType.CONTEXT_NAME_TEST, testcaseCompilette.TestName, null, Precedence.None),
                (TokenType.CONTEXT_NAME_TESTSET, testdec.TestSetName, null, Precedence.None),
                (TokenType.TYPEOF, TypeOf, null, Precedence.Term),
                (TokenType.MEETS, null, Meets, Precedence.Comparison),
                (TokenType.SIGNS, null, Signs, Precedence.Comparison),
                (TokenType.FUNCTION, FunExp, null, Precedence.Call),
                (TokenType.COUNT_OF, CountOf, null, Precedence.None)
                              );
        }

        public void Reset()
        {
            _compilerStates.Clear();
            TokenIterator = null;
            _compilingScript = null;
        }

        public void ThrowCompilerException(string msg)
        {
            throw new CompilerException(MessageFromContext(msg));
        }

        private string MessageFromContext(string msg)
        {
            var location = ChunkToLocationStr(CurrentChunk);
            var previousToken = TokenIterator.PreviousToken;
            var (line, character) = TokenIterator.GetLineAndCharacter(previousToken.StringSourceIndex);
            msg = msg + $" in {location} at {line}:{character}{LiteralStringPartial(previousToken.Literal)}.";
            return msg;
        }

        public void CompilerMessage(string msg)
        {
            _compilingScript.CompilerMessages.Add(new CompilerMessage(MessageFromContext(msg)));
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
            => _declarationCompilettes[compilette.MatchingToken] = compilette;

        public void AddStatementCompilette(ICompilette compilette)
            => _statementCompilettes[compilette.MatchingToken] = compilette;

        public void SetPrattRule(TokenType tt, IParseRule rule)
            => _prattParser.SetPrattRule(tt, rule);

        public void EmitPacket(ByteCodePacket packet)
        {
            //NOTE: this is slow but changing it doesn't change profiler times much.
            var (line, _) = TokenIterator.GetLineAndCharacter(TokenIterator.PreviousToken.StringSourceIndex);
            CurrentChunk.WritePacket(packet, line);
        }

        public void EmitNULL()
            => EmitPacket(new ByteCodePacket(OpCode.PUSH_VALUE, (byte)PushValueOpType.Null));

        public void EmitPushValue(short s)
            => EmitPacket(new ByteCodePacket(OpCode.PUSH_VALUE, (byte)PushValueOpType.Short, s));

        public void EmitPushValue(bool b)
            => EmitPacket(new ByteCodePacket(OpCode.PUSH_VALUE, (byte)PushValueOpType.Bool, (byte)(b ? 1 : 0)));

        public byte AddStringConstant()
            => AddCustomStringConstant((string)TokenIterator.PreviousToken.Literal);

        public void AddConstantDoubleAndWriteOp(double dbl)
        {
            var at = CurrentChunk.AddConstant(Value.New(dbl));  // always a double
            //NOTE: this is slow but changing it doesn't change profiler times much.
            var (line, _) = TokenIterator.GetLineAndCharacter(TokenIterator.PreviousToken.StringSourceIndex);
            CurrentChunk.WritePacket(new ByteCodePacket(OpCode.PUSH_CONSTANT, at, 0, 0), line);
        }

        public void AddConstantStringAndWriteOp(string str)
        {
            var at = AddCustomStringConstant(str);
            //NOTE: this is slow but changing it doesn't change profiler times much.
            var (line, _) = TokenIterator.GetLineAndCharacter(TokenIterator.PreviousToken.StringSourceIndex);
            CurrentChunk.WritePacket(new ByteCodePacket(OpCode.PUSH_CONSTANT, at, 0, 0), line);
        }

        public byte AddCustomStringConstant(string str)
            => CurrentChunk.AddConstant(Value.New(str));    //always a string

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
                var loc = comp.locals[comp.localCount - 1];

                if (loc.IsCaptured)
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

                if (loc.Depth > 0 && !loc.IsAccessed)
                {
                    CompilerMessage($"Local '{loc.Name}' is unused.");
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

        public CompiledScript Compile(TokenisedScript tokenisedScript)
        {
            _compilingScript = new CompiledScript(tokenisedScript.SourceScript.ScriptHash);
            TokenIterator = new TokenIterator(tokenisedScript, this, this);
            TokenIterator.Advance();

            PushCompilerState("root", FunctionType.Script);

            while (TokenIterator.CurrentToken.TokenType != TokenType.EOF)
            {
                Declaration();
            }

            var topChunk = EndCompile();
            return _compilingScript;
        }

        public void Declaration()
        {
            if (_declarationCompilettes.TryGetValue(TokenIterator.CurrentToken.TokenType, out var complette))
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
            if (_statementCompilettes.TryGetValue(TokenIterator.CurrentToken.TokenType, out var complette))
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
            => TokenIterator.Consume(TokenType.END_STATEMENT, $"Expect ; after {after}");

        public void PushCompilerState(string name, FunctionType functionType)
        {
            var parentChainName = string.Empty;
            if (CurrentCompilerState?.chunk?.ContainingChunkChainName != null
                && CurrentCompilerState?.chunk?.ContainingChunkChainName != null)
            {
                parentChainName = $"{CurrentCompilerState?.chunk?.ContainingChunkChainName}.{CurrentCompilerState?.chunk?.ChunkName}";
            }

            var newCompState = new CompilerState(CurrentCompilerState, functionType)
            {
                chunk = new Chunk(name, TokenIterator?.SourceName, parentChainName),
            };
            _compilerStates.Push(newCompState);
            CurrentCompilerState.AddLocal(this, "", 0);
        }

        //todo mark usage so we can emit warnings for un used or write but not read
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

            argId = AddCustomStringConstant(name);
            return new ResolveNameLookupResult(OpCode.FETCH_GLOBAL, OpCode.ASSIGN_GLOBAL, (byte)argId);
        }

        public Chunk EndCompile()
        {
            EmitReturn();
            EndScope();
            var returnChunk = _compilerStates.Pop().chunk;
            _compilingScript.AllChunks.Add(returnChunk);
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

        public byte FuncArgsAndReturns()
        {
            BeginScope();
            VariableNameListDeclareOptional(() => IncreaseArity(AddStringConstant()));
            var returnCount = VariableNameListDeclareOptional(() => IncreaseReturn(AddStringConstant()));
            return returnCount;
        }

        public void AutoDefineRetval()
        {
            var retvalId = DeclareAndDefineCustomVariable("retval");
            CurrentCompilerState.locals[CurrentCompilerState.localCount - 1].IsAccessed = true;
            IncreaseReturn(retvalId);
        }

        public Chunk Function(string functionName, FunctionType functionType)
        {
            PushCompilerState(functionName, functionType);

            var returnCount = FuncArgsAndReturns();

            if (returnCount == 0)
            {
                AutoDefineRetval();

            }

            // The body.
            TokenIterator.Consume(TokenType.OPEN_BRACE, "Expect '{' before function body.");
            Block();

            // Create the function object.
            var comp = CurrentCompilerState;   //we need this to mark upvalues
            var function = EndCompile();
            EmitPacket(new ByteCodePacket(new ByteCodePacket.ClosureDetails(ClosureType.Closure, CurrentChunk.AddConstant(Value.New(function)), (byte)function.UpvalueCount)));

            for (int i = 0; i < function.UpvalueCount; i++)
            {
                EmitPacket(
                    new ByteCodePacket(new ByteCodePacket.ClosureDetails(
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
            if (CurrentChunk.ArgumentConstantIds.Count > 255)
                ThrowCompilerException($"Can't have more than 255 parameters.");
        }

        public void IncreaseReturn(byte argNameConstant)
        {
            CurrentChunk.ReturnConstantIds.Add(argNameConstant);
            if (CurrentChunk.ReturnConstantIds.Count > 255)
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

        internal Label GotoUniqueChunkLabel(string v)
        {
            var labelNameID = CreateUniqueChunkLabel(v);
            EmitGoto(labelNameID);
            return labelNameID;
        }

        internal Label GotoIfUniqueChunkLabel(string v)
        {
            var labelNameID = CreateUniqueChunkLabel(v);
            EmitGotoIf(labelNameID);
            return labelNameID;
        }

        internal Label CreateUniqueChunkLabel(string v)
        {
            return CurrentCompilerState.chunk.AddLabel(new HashedString($"{v}_{CurrentChunk.Labels.Count}"),0);
        }

        public Label LabelUniqueChunkLabel(string v)
        {
            var labelNameID = CreateUniqueChunkLabel(v);
            EmitGoto(labelNameID);
            EmitLabel(labelNameID);
            return labelNameID;
        }

        public void EmitLabel(Label id)
        {
            CurrentCompilerState.chunk.AddLabel(id, CurrentChunkInstructionCount);
            EmitPacket(new ByteCodePacket(OpCode.LABEL, new ByteCodePacket.LabelDetails(id)));
        }

        internal void EmitGoto(Label labelNameID)
        {
            EmitPacket(new ByteCodePacket(OpCode.GOTO, new ByteCodePacket.LabelDetails(labelNameID)));
        }

        internal void EmitGotoIf(Label labelNameID)
        {
            EmitPacket(new ByteCodePacket(OpCode.GOTO_IF_FALSE, new ByteCodePacket.LabelDetails(labelNameID)));
        }

        internal void EmitPop(byte popCount = 1)
        {
            EmitPacket(new ByteCodePacket(OpCode.POP, popCount));
        }

        internal string IdentifierOrChunkUnique(string prefix)
        {
            if (TokenIterator.Match(TokenType.IDENTIFIER))
                return TokenIterator.PreviousToken.Literal;

            return ChunkUniqueName(prefix);
        }

        private string ChunkUniqueName(string prefix)
        {
            var existingPrefixes = CurrentChunk.Constants.Count(x => x.type == ValueType.String && x.val.asString.String.Contains(prefix));
            return $"{prefix}{CurrentChunk.SourceName}{existingPrefixes}";
        }

        internal byte ResolveLocal(string argName)
        {
            return (byte)CurrentCompilerState.ResolveLocal(this, argName);
        }

        public bool IsInClass()
        {
            return _classCompiler.CurrentTypeInfoEntry != null;
        }

        public bool DoesCurrentClassHaveMatchingField(string x)
        {
            return _classCompiler.CurrentTypeInfoEntry.Fields.Contains(x);
        }

        public string UniqueLocalName(string prefix)
        {
            return ChunkUniqueName(prefix);
        }

        private static string LiteralStringPartial(object literal)
        {
            if (literal == null) return string.Empty;

            var str = literal.ToString();

            if (string.IsNullOrEmpty(str)) return string.Empty;
            return $" '{str}'";
        }

        public static string ChunkToLocationStr(Chunk chunk)
        {
            return $"chunk '{chunk.GetLocationString()}'";
        }

        public static void FunctionDeclaration(Compiler compiler)
        {
            InnerFunctionDeclaration(compiler, true);
        }

        public static void VarDeclaration(Compiler compiler)
        {
            if (compiler.TokenIterator.Match(TokenType.OPEN_PAREN))
                MultiVarAssignToReturns(compiler);
            else
                PlainVarDeclare(compiler);

            compiler.ConsumeEndStatement();
        }

        private static void PlainVarDeclare(Compiler compiler)
        {
            do
            {
                var id = compiler.ParseVariable("Expect variable name");

                if (compiler.TokenIterator.Match(TokenType.ASSIGN))
                    compiler.Expression();
                else
                    compiler.EmitNULL();

                compiler.DefineVariable(id);
            } while (compiler.TokenIterator.Match(TokenType.COMMA));
        }

        private static void MultiVarAssignToReturns(Compiler compiler)
        {
            var varNames = new List<string>();
            do
            {
                compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect identifier within multivar declaration.");
                varNames.Add((string)compiler.TokenIterator.PreviousToken.Literal);
            } while (compiler.TokenIterator.Match(TokenType.COMMA));

            compiler.TokenIterator.Consume(TokenType.CLOSE_PAREN, "Expect ')' to end a multivar declaration.");
            compiler.TokenIterator.Consume(TokenType.ASSIGN, "Expect '=' after multivar declaration.");

            //mark stack start
            compiler.EmitPacket(new ByteCodePacket(OpCode.MULTI_VAR, 1));

            compiler.Expression();

            compiler.EmitPacket(new ByteCodePacket(OpCode.MULTI_VAR, 0, (byte)varNames.Count));

            if (compiler.CurrentCompilerState.scopeDepth == 0)
            {
                for (int i = varNames.Count - 1; i >= 0; i--)
                {
                    var varName = varNames[i];
                    compiler.DeclareAndDefineCustomVariable(varName);
                }
            }
            else
            {
                for (int i = 0; i < varNames.Count; i++)
                {
                    var varName = varNames[i];
                    compiler.DeclareAndDefineCustomVariable(varName);
                }
            }
        }
         public static void Unary(Compiler compiler, bool canAssign)
        {
            var op = compiler.TokenIterator.PreviousToken.TokenType;

            compiler.ParsePrecedence(Precedence.Unary);

            switch (op)
            {
            case TokenType.MINUS: compiler.EmitPacket(new ByteCodePacket(OpCode.NEGATE)); break;
            case TokenType.BANG: compiler.EmitPacket(new ByteCodePacket(OpCode.NOT)); break;
            default:
                break;
            }
        }

        public static void Binary(Compiler compiler, bool canAssign)
        {
            TokenType operatorType = compiler.TokenIterator.PreviousToken.TokenType;

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

        public static void Literal(Compiler compiler, bool canAssign)
        {
            switch (compiler.TokenIterator.PreviousToken.TokenType)
            {
            case TokenType.TRUE: compiler.EmitPushValue(true); break;
            case TokenType.FALSE: compiler.EmitPushValue(false); break;
            case TokenType.NULL: compiler.EmitNULL(); break;
            case TokenType.NUMBER:
            {
                var number = double.Parse(compiler.TokenIterator.PreviousToken.Literal);

                var isInt = number == Math.Truncate(number);

                if (isInt && number < short.MaxValue && number >= short.MinValue)
                {
                    compiler.EmitPushValue((short)number);
                    return;
                }

                var (isQuotientable, num, dem) = IsNumberQuotientable(number);
                if (isQuotientable)
                {
                    compiler.EmitPacket(new ByteCodePacket(new ByteCodePacket.QuotientDetails(num, dem)));
                    return;
                }

                compiler.AddConstantDoubleAndWriteOp(number);
            }
            break;

            case TokenType.STRING:
            {
                var str = (string)compiler.TokenIterator.PreviousToken.Literal;
                compiler.AddConstantStringAndWriteOp(str);
            }
            break;
            }
        }

        public static (bool, short, byte) IsNumberQuotientable(double number)
        {
            var (isPossible, nume, denom) = DoubleToQuotient.ToQuotient(number, 10);
            if (!isPossible)
            {
                return (false, 0, 0);
            }

            if (nume <= short.MaxValue 
                && nume >= short.MinValue
                && denom <= byte.MaxValue)
            {
                return (true, (short)nume, (byte)denom);
            }

            return (false, 0, 0);
        }

        public static void Variable(Compiler compiler, bool canAssign)
        {
            var name = (string)compiler.TokenIterator.PreviousToken.Literal;
            compiler.NamedVariable(name, canAssign);
        }

        public static void And(Compiler compiler, bool canAssign)
        {
            var endJumpLabel = compiler.GotoIfUniqueChunkLabel("after_and");

            compiler.EmitPop();
            compiler.ParsePrecedence(Precedence.And);

            compiler.EmitGoto(endJumpLabel);
            compiler.EmitLabel(endJumpLabel);
        }

        public static void Or(Compiler compiler, bool canAssign)
        {
            var elseJumpLabel = compiler.GotoIfUniqueChunkLabel("short_or");
            var endJump = compiler.GotoUniqueChunkLabel("after_or");

            compiler.EmitLabel(elseJumpLabel);
            compiler.EmitPop();

            compiler.ParsePrecedence(Precedence.Or);

            compiler.EmitGoto(endJump);
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
                ? compiler.TokenIterator.PreviousToken.Literal
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

        public static void BracketCreate(Compiler compiler, bool canAssign)
        {
            compiler.EmitPacket(new ByteCodePacket(OpCode.NATIVE_TYPE, NativeType.List));

            while (!compiler.TokenIterator.Check(TokenType.CLOSE_BRACKET))
            {
                compiler.Expression();

                var constantNameId = compiler.AddCustomStringConstant("Add");
                compiler.EmitPacket(new ByteCodePacket(OpCode.INVOKE, constantNameId, 1, 0));
                compiler.TokenIterator.Match(TokenType.COMMA);
            }

            compiler.TokenIterator.Consume(TokenType.CLOSE_BRACKET, $"Expect ']' after list.");
        }

        public static void BraceCreateDynamic(Compiler compiler, bool arg2)
        {
            var midTok = TokenType.ASSIGN;
            if (compiler.TokenIterator.Check(TokenType.IDENTIFIER))
            {
                compiler.EmitPacket(new ByteCodePacket(OpCode.NATIVE_TYPE, NativeType.Dynamic));

                while (!compiler.TokenIterator.Match(TokenType.CLOSE_BRACE))
                {
                    //we need to copy the dynamic inst
                    compiler.EmitPacket(new ByteCodePacket(OpCode.DUPLICATE));  //todo this is now the only thing that uses this
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
                //TODO: expects others to have already been desugared, not sure that's a win
                compiler.ThrowCompilerException("Expect identifier or '=' after '{'");
            }
        }

        public static void CountOf(Compiler compiler, bool canAssign)
        {
            compiler.Expression();
            compiler.EmitPacket(new ByteCodePacket(OpCode.COUNT_OF));
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

        public static void FName(Compiler compiler, bool canAssign)
        {
            var fname = compiler.CurrentChunk.ChunkName;
            compiler.AddConstantStringAndWriteOp(fname);
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

        public static void TypeOf(Compiler compiler, bool canAssign)
        {
            compiler.TokenIterator.Consume(TokenType.OPEN_PAREN, "Expect '(' after typeof.");
            compiler.Expression();
            compiler.TokenIterator.Consume(TokenType.CLOSE_PAREN, "Expect ')' after typeof.");
            compiler.EmitPacket(new ByteCodePacket(OpCode.TYPEOF));
        }

         public static void BuildStatement(Compiler compiler)
        {
            do
            {
                compiler.Expression();
                compiler.EmitPacket(new ByteCodePacket(OpCode.BUILD));
            } while (compiler.TokenIterator.Match(TokenType.COMMA));

            compiler.ConsumeEndStatement("build command identifier(s)");
        }

        public static void IfStatement(Compiler compiler)
        {
            compiler.TokenIterator.Consume(TokenType.OPEN_PAREN, "Expect '(' after if.");
            compiler.Expression();
            compiler.TokenIterator.Consume(TokenType.CLOSE_PAREN, "Expect ')' after if.");

            //todo can we make goto_if consume the value in the vm so we don't need to play pop wackamole
            var wasFalseLabel = compiler.GotoIfUniqueChunkLabel("if_false");
            compiler.EmitPop();

            compiler.Statement();

            var afterIfLabel = compiler.GotoUniqueChunkLabel("if_end");

            if (compiler.TokenIterator.Match(TokenType.ELSE))
            {
                var elseJump = compiler.GotoUniqueChunkLabel("else");

                compiler.EmitLabel(wasFalseLabel);
                compiler.EmitPop();

                compiler.Statement();

                compiler.EmitGoto(afterIfLabel);
                compiler.EmitLabel(elseJump);
            }
            else
            {
                compiler.EmitLabel(wasFalseLabel);
                compiler.EmitPop();
                compiler.EmitGoto(afterIfLabel);
            }

            compiler.EmitLabel(afterIfLabel);
        }

        public static void YieldStatement(Compiler compiler)
        {
            compiler.EmitPacket(new ByteCodePacket(OpCode.YIELD));

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

        public static void ContinueStatement(Compiler compiler)
        {
            var comp = compiler.CurrentCompilerState;
            if (comp.LoopStates.Count == 0)
                compiler.ThrowCompilerException($"Cannot continue when not inside a loop.");

            compiler.PopBackToScopeDepth(comp.LoopStates.Last().ScopeDepth);
            compiler.EmitGoto(comp.LoopStates.Peek().ContinueLabelID);
            compiler.ConsumeEndStatement();
        }

        public static void BlockStatement(Compiler compiler)
            => compiler.BlockStatement();

        public static void PanicStatement(Compiler compiler)
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
            compiler.EmitPacket(new ByteCodePacket(OpCode.PANIC));
        }

        public static void NoOpStatement(Compiler compiler)
        {
        }

        //todo expects be desugar
        //could be come if (!(exp)) throw "Expects failed, '{msg}'"
        //problem is we don't know what an exp or statement is yet, tokens would need to either be ast or know similar for
        //  us to be able to scan ahead and reorder them correctly
        public static void ExpectStatement(Compiler compiler)
        {
            do
            {
                //find start of the string so we can later substr it if desired
                var startIndex = compiler.TokenIterator.PreviousToken.StringSourceIndex + 1;
                compiler.Expression();
                compiler.EmitPacket(new ByteCodePacket(OpCode.NOT));
                var thenjumpLabel = compiler.GotoIfUniqueChunkLabel("if_false");
                compiler.EmitPop();

                compiler.AddConstantStringAndWriteOp("Expect failed, '");

                if (compiler.TokenIterator.Match(TokenType.COLON))
                {
                    compiler.Expression();
                }
                else
                {
                    var endIndex = compiler.TokenIterator.CurrentToken.StringSourceIndex;
                    var length = endIndex - startIndex;
                    var sourceStringSection = compiler.TokenIterator.GetSourceSection(startIndex, length);
                    compiler.AddConstantStringAndWriteOp(sourceStringSection.Trim());
                }

                compiler.AddConstantStringAndWriteOp("'");
                compiler.EmitPacket(new ByteCodePacket(OpCode.ADD));
                compiler.EmitPacket(new ByteCodePacket(OpCode.ADD));
                compiler.EmitPacket(new ByteCodePacket(OpCode.PANIC));
                compiler.EmitLabel(thenjumpLabel);
                compiler.EmitPop();

                //if trailing comma, eat it
                compiler.TokenIterator.Match(TokenType.COMMA);
            } while (!compiler.TokenIterator.Check(TokenType.END_STATEMENT));

            compiler.ConsumeEndStatement();
        }

        //todo match be sugar?
        //could become if (a) statement elseif (b) statement // else throw $"Match on '{matchArgName}' did have a matching case."
        public static void MatchStatement(Compiler compiler)
        {
            //make a scope
            compiler.BeginScope();

            compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect identifier after match statement.");
            var matchArgName = compiler.TokenIterator.PreviousToken.Literal;
            var resolveRes = compiler.ResolveNameLookupOpCode(matchArgName);

            var lastElseLabel = Label.Default;

            var matchEndLabelID = compiler.CreateUniqueChunkLabel(nameof(MatchStatement));

            compiler.TokenIterator.Consume(TokenType.OPEN_BRACE, "Expect '{' after match expression.");
            do
            {
                if (lastElseLabel != Label.Default)
                {
                    compiler.EmitLabel(lastElseLabel);
                    compiler.EmitPop();
                }

                compiler.Expression();
                compiler.EmitPacketFromResolveGet(resolveRes);
                compiler.EmitPacket(new ByteCodePacket(OpCode.EQUAL));
                lastElseLabel = compiler.GotoIfUniqueChunkLabel("match");
                compiler.EmitPop();
                compiler.TokenIterator.Consume(TokenType.COLON, "Expect ':' after match case expression.");
                compiler.Statement();
                compiler.EmitGoto(matchEndLabelID);
            } while (!compiler.TokenIterator.Match(TokenType.CLOSE_BRACE));

            if (lastElseLabel != Label.Default)
                compiler.EmitLabel(lastElseLabel);

            compiler.AddConstantStringAndWriteOp($"Match on '{matchArgName}' did have a matching case.");
            compiler.EmitPacket(new ByteCodePacket(OpCode.PANIC));

            compiler.EmitLabel(matchEndLabelID);

            compiler.EndScope();
        }

        public static void LabelStatement(Compiler compiler)
        {
            compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect identifier after 'label' statement.");
            var labelName = compiler.TokenIterator.PreviousToken.Literal;
            var id = compiler.CurrentChunk.AddLabel(new HashedString(labelName), compiler.CurrentChunkInstructionCount);//TODO this is just wrong change to add custom label
            compiler.EmitGoto(id);  //we require that you cannot stumble into a label so if you request one you need to go to it immediately
            compiler.EmitLabel(id);

            compiler.ConsumeEndStatement();
        }

        public static void GotoStatement(Compiler compiler)
        {
            compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect identifier after 'goto' statement.");
            var labelNameID = compiler.CurrentChunk.CreateLabel(new HashedString(compiler.TokenIterator.PreviousToken.Literal));
            compiler.EmitGoto(labelNameID);

            compiler.ConsumeEndStatement();
        }

        public static void ReadOnlyStatement(Compiler compiler)
        {
            compiler.Expression();
            compiler.EmitPacket(new ByteCodePacket(OpCode.READ_ONLY));

            compiler.ConsumeEndStatement();
        }

        public static void ReturnStatement(Compiler compiler)
        {
            if (compiler.CurrentCompilerState.functionType == FunctionType.Init)
                compiler.ThrowCompilerException("Cannot return an expression from an 'init'");

            compiler.EmitReturn();

            compiler.ConsumeEndStatement();
        }

        public static void ForStatement(Compiler compiler)
        {
            compiler.BeginScope();

            var comp = compiler.CurrentCompilerState;
            var loopState = new LoopState(compiler.CreateUniqueChunkLabel("loop_exit"));
            comp.LoopStates.Push(loopState);

            //preloop
            compiler.TokenIterator.Consume(TokenType.OPEN_PAREN, "Expect '(' after loop with conditions.");
            //we really only want a var decl, var assign, or empty but Declaration covers everything
            compiler.Declaration();

            loopState.StartLabelID = compiler.LabelUniqueChunkLabel("loop_start");
            loopState.ContinueLabelID = loopState.StartLabelID;

            //begine loop
            var hasCondition = false;
            //condition
            {
                if (!compiler.TokenIterator.Check(TokenType.END_STATEMENT))
                {
                    hasCondition = true;
                    compiler.Expression();
                    loopState.HasExit = true;
                }
                compiler.ConsumeEndStatement("loop condition");

                // Jump out of the loop if the condition is false.
                compiler.EmitGotoIf(loopState.ExitLabelID);
                if (hasCondition)
                    compiler.EmitPop(); // Condition.
            }

            var bodyJump = compiler.GotoUniqueChunkLabel("loop_body");
            //increment
            {
                var newStartLabel = compiler.LabelUniqueChunkLabel("loop_continue");
                loopState.ContinueLabelID = newStartLabel;
                if (compiler.TokenIterator.CurrentToken.TokenType != TokenType.CLOSE_PAREN)
                {
                    compiler.Expression();
                    compiler.EmitPop();
                }
                compiler.EmitGoto(loopState.StartLabelID);
                loopState.StartLabelID = newStartLabel;
                compiler.EmitLabel(bodyJump);
            }

            compiler.TokenIterator.Consume(TokenType.CLOSE_PAREN, "Expect ')' after loop clauses.");

            compiler.BeginScope();
            loopState.ScopeDepth = comp.scopeDepth;
            compiler.Statement();
            compiler.EndScope();

            compiler.EmitGoto(loopState.StartLabelID);

            if (!loopState.HasExit)
                compiler.ThrowCompilerException("Loops must contain a termination");
            compiler.EmitLabel(loopState.ExitLabelID);
            compiler.EmitPop();

            compiler.EndScope();
        }
    }
}
