using System.Collections.Generic;

namespace ULox
{
    public sealed class ClassTypeCompilette : TypeCompilette
    {
        public static readonly HashedString InitMethodName = new HashedString("init");
        public static readonly HashedString ThisName = new HashedString("this");

        private readonly Dictionary<TokenType, ITypeBodyCompilette> _innerDeclarationCompilettes = new Dictionary<TokenType, ITypeBodyCompilette>();
        private ITypeBodyCompilette _bodyCompiletteFallback;

        public override TokenType MatchingToken => TokenType.CLASS;

        public override UserType UserType => UserType.Class;

        public ClassTypeCompilette()
        {
            AddInnerDeclarationCompilette(new TypeStaticElementCompilette(this));
            AddInnerDeclarationCompilette(new TypeInitCompilette(this));
            AddInnerDeclarationCompilette(new TypeMethodCompilette(this));
            AddInnerDeclarationCompilette(new TypeSignsCompilette(this));
            AddInnerDeclarationCompilette(new TypeMixinCompilette(this));
            AddInnerDeclarationCompilette(new TypeInstancePropertyCompilette(this));
        }

        public void This(Compiler compiler, bool canAssign)
        {
            if (CurrentTypeName == null)
                compiler.ThrowCompilerException("Cannot use the 'this' keyword outside of a class");

            compiler.NamedVariable(ThisName.String, canAssign);
        }

        private void AddInnerDeclarationCompilette(ITypeBodyCompilette compilette)
        {
            _innerDeclarationCompilettes[compilette.MatchingToken] = compilette;
            if (compilette.MatchingToken == TokenType.NONE)
                _bodyCompiletteFallback = compilette;
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
            compilette.Process(compiler);
        }

        private void ValidStage(Compiler compiler, TypeCompiletteStage stage)
        {
            if (Stage > stage)
                compiler.ThrowCompilerException($"Stage out of order. Type '{CurrentTypeName}' is at stage '{Stage}' has encountered a late '{stage}' stage element");

            Stage = stage;
        }

        private void CompileMethod(Compiler compiler, FunctionType functionType)
        {
            byte constant = compiler.AddStringConstant();
            var name = compiler.TokenIterator.PreviousToken.Lexeme;
            var chunk = compiler.Function(name, functionType);
            compiler.EmitPacket(new ByteCodePacket(OpCode.METHOD, constant, 0, 0)); //todo: remove once we bind types
            CurrentTypeInfoEntry.AddMethod(chunk);
        }

        public sealed class TypeStaticElementCompilette : ITypeBodyCompilette
        {
            private ClassTypeCompilette _classTypeCompilette;

            public TypeStaticElementCompilette(ClassTypeCompilette classTypeCompilette)
            {
                _classTypeCompilette = classTypeCompilette;
            }

            public TokenType MatchingToken
                => TokenType.STATIC;

            public TypeCompiletteStage Stage
                => TypeCompiletteStage.Static;

            public void Process(Compiler compiler)
            {
                if (compiler.TokenIterator.Match(TokenType.VAR))
                    StaticProperty(compiler);
                else
                    StaticMethod(compiler);
            }

            private static void StaticProperty(Compiler compiler)
            {
                do
                {
                    compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect var name");
                    byte nameConstant = compiler.AddStringConstant();

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
                } while (compiler.TokenIterator.Match(TokenType.COMMA));

                compiler.ConsumeEndStatement();
            }

            private void StaticMethod(Compiler compiler)
            {
                compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect method name");
                _classTypeCompilette.CompileMethod(compiler, FunctionType.Function);
            }
        }
        public sealed class TypeInitCompilette : ITypeBodyCompilette
        {
            private ClassTypeCompilette _classTypeCompilette;

            public TypeInitCompilette(ClassTypeCompilette classTypeCompilette)
            {
                _classTypeCompilette = classTypeCompilette;
            }

            public TokenType MatchingToken
                => TokenType.INIT;

            public TypeCompiletteStage Stage
                => TypeCompiletteStage.Init;

            public void Process(Compiler compiler)
            {
                _classTypeCompilette.CompileMethod(compiler, FunctionType.Init);
            }
        }
        public sealed class TypeMethodCompilette : ITypeBodyCompilette
        {
            private ClassTypeCompilette _classTypeCompilette;

            public TypeMethodCompilette(ClassTypeCompilette classTypeCompilette)
            {
                _classTypeCompilette = classTypeCompilette;
            }

            public TokenType MatchingToken
                => TokenType.NONE;

            public TypeCompiletteStage Stage
                => TypeCompiletteStage.Method;

            public void Process(Compiler compiler)
            {
                compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect method name");
                _classTypeCompilette.CompileMethod(compiler, FunctionType.Method);
            }
        }
        public sealed class TypeInstancePropertyCompilette : ITypeBodyCompilette
        {
            private readonly TypeCompilette _typeCompilette;

            public TokenType MatchingToken
                => TokenType.VAR;

            public TypeCompiletteStage Stage
                => TypeCompiletteStage.Var;

            public TypeInstancePropertyCompilette(TypeCompilette typeCompilette)
            {
                _typeCompilette = typeCompilette;
            }

            public void Process(Compiler compiler)
            {
                do
                {
                    compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect var name");
                    byte nameConstant = compiler.AddStringConstant();

                    compiler.NamedVariable(_typeCompilette.CurrentTypeName, false);
                    compiler.EmitPacket(new ByteCodePacket(OpCode.FIELD, nameConstant));    //TODO: once we make types this would go away, the fields would be setup by type and assigned by initchains
                    _typeCompilette.CurrentTypeInfoEntry.AddField(compiler.TokenIterator.PreviousToken.Lexeme);

                    //emit jump // to skip this during imperative
                    var initFragmentJump = compiler.GotoUniqueChunkLabel("SkipInitDuringImperative");
                    //patch jump previous init fragment if it exists
                    if (_typeCompilette.PreviousInitFragLabelId != -1)
                    {
                        compiler.EmitLabel((byte)_typeCompilette.PreviousInitFragLabelId);
                    }
                    else
                    {
                        compiler.EmitLabel((byte)_typeCompilette.InitChainLabelId);
                    }

                    compiler.EmitPacket(new ByteCodePacket(OpCode.GET_LOCAL, (byte)0));//get class or inst this on the stack

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
                    //emit jump // to move to next prop init fragment, defaults to jump nowhere return
                    _typeCompilette.PreviousInitFragLabelId = compiler.GotoUniqueChunkLabel("InitFragmentJump");

                    //patch jump from skip imperative
                    compiler.EmitLabel(initFragmentJump);

                    //if trailing comma, eat it
                    compiler.TokenIterator.Match(TokenType.COMMA);
                } while (compiler.TokenIterator.Check(TokenType.IDENTIFIER));

                compiler.TokenIterator.Match(TokenType.END_STATEMENT);
            }
        }
        public sealed class TypeMixinCompilette : ITypeBodyCompilette
        {
            private readonly TypeCompilette _typeCompilette;

            public TokenType MatchingToken
                => TokenType.MIXIN;
            public TypeCompiletteStage Stage
                => TypeCompiletteStage.Mixin;

            public TypeMixinCompilette(TypeCompilette typeCompilette)
            {
                _typeCompilette = typeCompilette;
            }

            public void Process(Compiler compiler)
            {
                do
                {
                    compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect identifier after mixin into class.");
                    var targetname = compiler.TokenIterator.PreviousToken.Literal as string;
                    compiler.NamedVariable(targetname, false);
                    var targetTypeInfoEntry = compiler.TypeInfo.GetUserType(targetname);
                    _typeCompilette.CurrentTypeInfoEntry.AddMixin(targetTypeInfoEntry);
                    compiler.NamedVariable(_typeCompilette.CurrentTypeName, false);
                    compiler.EmitPacket(new ByteCodePacket(OpCode.MIXIN));  //todo: remove once we bind types
                } while (compiler.TokenIterator.Match(TokenType.COMMA));

                compiler.ConsumeEndStatement("mixin declaration");
            }
        }
        public sealed class TypeSignsCompilette : ITypeBodyCompilette
        {
            private readonly TypeCompilette _typeCompilette;

            public TokenType MatchingToken
                => TokenType.SIGNS;
            public TypeCompiletteStage Stage
                => TypeCompiletteStage.Signs;

            public TypeSignsCompilette(TypeCompilette typeCompilette)
            {
                _typeCompilette = typeCompilette;
            }

            public void Process(Compiler compiler)
            {
                var contractNames = new List<string>();
                do
                {
                    compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect identifier after signs into class.");
                    contractNames.Add(compiler.TokenIterator.PreviousToken.Literal as string);
                } while (compiler.TokenIterator.Match(TokenType.COMMA));

                compiler.ConsumeEndStatement("signs declaration");

                _typeCompilette.OnPostBody += (_) =>
                {
                    foreach (var contractName in contractNames)
                    {
                        _typeCompilette.CurrentTypeInfoEntry.AddContract(contractName);
                        var (meets, msg) = MeetValidator.ValidateClassMeetsClass(_typeCompilette.CurrentTypeInfoEntry, compiler.TypeInfo.GetUserType(contractName));
                        if(!meets)
                        {
                            compiler.ThrowCompilerException($"Class '{_typeCompilette.CurrentTypeName}' does not meet contract '{contractName}'. {msg}");
                        }
                    }
                };
            }
        }
    }
}
