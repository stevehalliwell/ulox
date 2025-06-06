﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

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
                (TokenType.CONTEXT_NAME_TEST, testcaseCompilette.TestName, null, Precedence.None),
                (TokenType.CONTEXT_NAME_TESTSET, testdec.TestSetName, null, Precedence.None),
                (TokenType.TYPEOF, CompilerExpressions.TypeOf, null, Precedence.Term),
                (TokenType.MEETS, null, CompilerExpressions.Meets, Precedence.Comparison),
                (TokenType.SIGNS, null, CompilerExpressions.Signs, Precedence.Comparison),
                (TokenType.FUNCTION, CompilerExpressions.FunExp, null, Precedence.Call),
                (TokenType.COUNT_OF, CompilerExpressions.CountOf, null, Precedence.None)
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
    }
}
