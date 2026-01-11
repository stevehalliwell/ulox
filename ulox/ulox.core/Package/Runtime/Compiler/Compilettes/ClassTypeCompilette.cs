using System;
using System.Collections.Generic;

namespace ULox
{
    public sealed class ClassTypeCompilette : TypeCompilette
    {
        public static readonly HashedString InitMethodName = new("init");
        public static readonly HashedString ThisName = new("this");

        private readonly Dictionary<TokenType, (TypeCompiletteStage Stage, Action<Compiler> process)> _innerDeclarationCompilettes;
        private (TypeCompiletteStage Stage, Action<Compiler> process) _bodyCompiletteFallback;

        public override TokenType MatchingToken => TokenType.CLASS;

        public override UserType UserType => UserType.Class;

        private bool _needsEndClosure = false;
        private Label _initFragmentJump = Label.Default;

        public override bool EmitClosureCallAtEnd => _needsEndClosure;

        public ClassTypeCompilette()
        {
            _bodyCompiletteFallback = (TypeCompiletteStage.Method, c => CompileMethod(c, FunctionType.Method));
            _innerDeclarationCompilettes = new Dictionary<TokenType, (TypeCompiletteStage, Action<Compiler>)>()
            {
                { TokenType.STATIC, (TypeCompiletteStage.Static, StaticElement) },
                { TokenType.VAR, (TypeCompiletteStage.Var, Property) },
                { TokenType.INIT, (TypeCompiletteStage.Init, c => CompileMethod(c, FunctionType.Init)) },
                { TokenType.MIXIN, (TypeCompiletteStage.Mixin, Mixin) },
                { TokenType.SIGNS, (TypeCompiletteStage.Signs, Signs) },
            };
        }

        public void This(Compiler compiler, bool canAssign)
        {
            if (CurrentTypeName == null)
                compiler.ThrowCompilerException("Cannot use the 'this' keyword outside of a class");

            compiler.NamedVariable(ThisName.String, canAssign);
        }

        protected override void Start()
        {
            _needsEndClosure = false;
            _initFragmentJump = Label.Default;
        }

        protected override void InnerBodyElement(Compiler compiler)
        {
            var compilette = _bodyCompiletteFallback;
            if (_innerDeclarationCompilettes.TryGetValue(compiler.TokenIterator.CurrentToken.TokenType, out var matchingCompilette))
            {
                compiler.TokenIterator.Advance();
                compilette = matchingCompilette;
            }

            ValidStage(compiler, compilette.Stage);
            compilette.process(compiler);
        }

        private void ValidStage(Compiler compiler, TypeCompiletteStage stage)
        {
            if (Stage > stage)
                compiler.ThrowCompilerException($"Stage out of order. Type '{CurrentTypeName}' is at stage '{Stage}' has encountered a late '{stage}' stage element");

            Stage = stage;
        }

        private void CompileMethod(Compiler compiler, FunctionType functionType)
        {
            if (functionType != FunctionType.Init)
                compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect method name");

            var _ = compiler.AddStringConstant();
            var methodName = compiler.TokenIterator.PreviousToken.Literal;

            compiler.PushCompilerState(methodName, functionType);

            if (functionType == FunctionType.Method
               || functionType == FunctionType.Init)
            {
                compiler.CurrentCompilerState.locals[0] = new CompilerState.Local(ThisName.String, 0);
            }

            byte returnCount = compiler.FuncArgsAndReturns();

            if (functionType == FunctionType.Init)
            {
                if (returnCount != 0)
                    compiler.ThrowCompilerException("Init functions cannot specify named return vars.");
            }
            else if (returnCount == 0)
            {
                compiler.AutoDefineRetval();
            }

            // The body.
            compiler.TokenIterator.Consume(TokenType.OPEN_BRACE, "Expect '{' before function body.");
            compiler.Block();

            var function = compiler.EndCompile();
            CurrentTypeInfoEntry.AddMethod(function);
        }

        private void StaticProperty(Compiler compiler)
        {
            do
            {
                compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect var name");
                byte nameConstant = compiler.AddStringConstant();
                CurrentTypeInfoEntry.AddStaticField(compiler.TokenIterator.PreviousToken.Literal);

                compiler.EmitPacket(new ByteCodePacket(OpCode.GET_LOCAL, 1));//get class or inst this on the stack

                //if = consume it and then
                //eat 1 expression or a push null
                if (compiler.TokenIterator.Match(TokenType.ASSIGN))
                {
                    compiler.Expression();
                }
                else
                {
                    compiler.EmitNULL();
                }

                //emit set prop
                compiler.EmitPacket(new ByteCodePacket(OpCode.SET_PROPERTY, nameConstant));
                compiler.EmitPop();
                _needsEndClosure = true;

                //if trailing comma, eat it
                compiler.TokenIterator.Match(TokenType.COMMA);
            } while (compiler.TokenIterator.Check(TokenType.IDENTIFIER));

            compiler.ConsumeEndStatement();
        }

        private void Property(Compiler compiler)
        {
            another_property:
            do
            {
                compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect var name");
                byte nameConstant = compiler.AddStringConstant();

                CurrentTypeInfoEntry.AddField(compiler.TokenIterator.PreviousToken.Literal);

                if (_initFragmentJump == Label.Default)
                {
                    //emit jump // to skip this during imperative
                    _initFragmentJump = compiler.GotoUniqueChunkLabel("SkipInitDuringImperative");
                    compiler.EmitLabel(InitChainLabelId);
                }

                compiler.EmitPacket(new ByteCodePacket(OpCode.GET_LOCAL, (byte)0));//get class or inst this on the stack

                //if = consume it and then
                //eat 1 expression or a push null
                if (compiler.TokenIterator.Match(TokenType.ASSIGN))
                    compiler.Expression();
                else
                    compiler.EmitNULL();

                //emit set prop
                compiler.EmitPacket(new ByteCodePacket(OpCode.SET_PROPERTY, nameConstant));
                compiler.EmitPop();

                //if trailing comma, eat it
                compiler.TokenIterator.Match(TokenType.COMMA);
            } while (compiler.TokenIterator.Check(TokenType.IDENTIFIER));

            //todo should this be consume?
            compiler.TokenIterator.Match(TokenType.END_STATEMENT);

            if (compiler.TokenIterator.Match(TokenType.VAR))
                goto another_property;

            PreviousInitFragLabelId = compiler.GotoUniqueChunkLabel("InitChainEnd");
            //patch jump from skip imperative
            compiler.EmitLabel(_initFragmentJump);
        }

        private void Mixin(Compiler compiler)
        {
            do
            {
                compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect identifier after mixin into class.");
                var targetname = compiler.TokenIterator.PreviousToken.Literal;
                var targetTypeInfoEntry = compiler.TypeInfo.GetUserType(targetname, compiler);
                CurrentTypeInfoEntry.AddMixin(targetTypeInfoEntry);
                compiler.TokenIterator.Match(TokenType.COMMA);
            } while (!compiler.TokenIterator.Check(TokenType.END_STATEMENT));

            compiler.ConsumeEndStatement("mixin declaration");
        }

        private void Signs(Compiler compiler)
        {
            var contractNames = new List<string>();
            do
            {
                compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect identifier after signs into class.");
                contractNames.Add(compiler.TokenIterator.PreviousToken.Literal);
                compiler.TokenIterator.Match(TokenType.COMMA);
            } while (!compiler.TokenIterator.Check(TokenType.END_STATEMENT));

            compiler.ConsumeEndStatement("signs declaration");

            OnPostBody += (_) =>
            {
                foreach (var contractName in contractNames)
                {
                    CurrentTypeInfoEntry.AddContract(contractName);
                    var (meets, msg) = MeetValidator.ValidateClassMeetsClass(CurrentTypeInfoEntry, compiler.TypeInfo.GetUserType(contractName, compiler));
                    if (!meets)
                    {
                        compiler.ThrowCompilerException($"Class '{CurrentTypeName}' does not meet contract '{contractName}'. {msg}");
                    }
                }
            };
        }

        private void StaticElement(Compiler compiler)
        {
            if (compiler.TokenIterator.Match(TokenType.VAR))
                StaticProperty(compiler);
            else
                CompileMethod(compiler, FunctionType.Function);
        }
    }
}
