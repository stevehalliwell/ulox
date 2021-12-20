using System.Collections.Generic;

namespace ULox
{
    public class ClassCompilette : ICompilette
    {
        public static readonly HashedString InitMethodName = new HashedString("init");

        protected Dictionary<TokenType, ICompilette> innerDeclarationCompilettes = new Dictionary<TokenType, ICompilette>();

        protected enum Stage { Invalid, Begin, Static, Mixin, Var, Init, Method, Complete }

        protected Stage _stage = Stage.Invalid;
        public string CurrentClassName { get; private set; }

        private List<string> classVarNames = new List<string>();
        private Stack<string> mixinNames = new Stack<string>();
        private int _initFragStartLocation = -1;
        private int _previousInitFragJumpLocation = -1;

        public void AddInnerDeclarationCompilette(ICompilette compilette)
            => innerDeclarationCompilettes[compilette.Match] = compilette;

        public TokenType Match => TokenType.CLASS;

        public void Process(CompilerBase compiler)
        {
            ClassDeclaration(compiler);
        }

        private void ClassDeclaration(CompilerBase compiler)
        {
            _initFragStartLocation = -1;
            _previousInitFragJumpLocation = -1;

            _stage = Stage.Begin;
            classVarNames.Clear();
            mixinNames.Clear();
            compiler.Consume(TokenType.IDENTIFIER, "Expect class name.");
            var className = (string)compiler.PreviousToken.Literal;
            var compState = compiler.CurrentCompilerState;
            CurrentClassName = className;

            byte nameConstant = compiler.AddStringConstant();
            compiler.DeclareVariable();
            EmitClassOp(compiler, nameConstant, out var initChainInstruction);
            compiler.DefineVariable(nameConstant);

            bool hasSuper = false;

            if (compiler.Match(TokenType.LESS))
            {
                hasSuper = DoClassInher(compiler, className, compState);
            }

            compiler.NamedVariable(className, false);
            compiler.Consume(TokenType.OPEN_BRACE, "Expect '{' before class body.");
            while (!compiler.Check(TokenType.CLOSE_BRACE) && !compiler.Check(TokenType.EOF))
            {
                if (compiler.Match(TokenType.STATIC))
                {
                    ValidStage(Stage.Static);
                    DoClassStatic(compiler);
                }
                else if (compiler.Match(TokenType.MIXIN))
                {
                    ValidStage(Stage.Mixin);
                    DoClassMixins(compiler);
                }
                else if (compiler.Match(TokenType.VAR))
                {
                    ValidStage(Stage.Var);
                    Property(compiler);
                }
                else if (compiler.Match(TokenType.INIT))
                {
                    ValidStage(Stage.Init);
                    DoInit(compiler);
                }
                else if (innerDeclarationCompilettes.TryGetValue(compiler.CurrentToken.TokenType, out var complette))
                {
                    ValidStage(Stage.Method);
                    compiler.Advance();
                    complette.Process(compiler);
                }
                else
                {
                    ValidStage(Stage.Method);
                    DoClassMethod(compiler);
                }
            }

            _stage = Stage.Complete;

            //dump all mixins after everything else so we don't have to fight regular class setup process in vm
            while(mixinNames.Count > 0)
            {
                var mixinName = mixinNames.Pop();
                compiler.NamedVariable(mixinName, false);
                compiler.NamedVariable(CurrentClassName, false);
                compiler.EmitOpAndBytes(OpCode.MIXIN);
            }


            //emit return //if we are the last link in the chain this ends our call

            if (_initFragStartLocation != -1)
            {
                compiler.WriteUShortAt(initChainInstruction, (ushort)_initFragStartLocation);
            }

            //return stub used by init and test chains
            var classReturnEnd = compiler.EmitJump(OpCode.JUMP);

            if (_previousInitFragJumpLocation != -1)
                compiler.PatchJump(_previousInitFragJumpLocation);

            compiler.EmitOpAndBytes(OpCode.RETURN, (byte)ReturnMode.One);

            compiler.PatchJump(classReturnEnd);

            compiler.Consume(TokenType.CLOSE_BRACE, "Expect '}' after class body.");
            compiler.EmitOpCode(OpCode.POP);

            if (hasSuper)
            {
                compiler.EndScope();
            }

            CurrentClassName = null;
        }

        private void ValidStage(Stage stage)
        {
            if(_stage > stage)
            {
                throw new CompilerException($"Class '{CurrentClassName}', encountered element of stage '{stage}' too late, class is at stage '{_stage}'. This is not allowed.");
            }
            _stage = stage;
        }

        private static void EmitClassOp(
            CompilerBase compiler,
            byte nameConstant,
            out short initInstructionLoc)
        {
            compiler.EmitOpAndBytes(OpCode.CLASS, nameConstant);
            initInstructionLoc = (short)compiler.CurrentChunk.Instructions.Count;
            compiler.EmitUShort(0);
        }

        private static bool DoClassInher(CompilerBase compiler, string className, CompilerState compState)
        {
            bool hasSuper;
            compiler.Consume(TokenType.IDENTIFIER, "Expect superclass name.");
            compiler.NamedVariableFromPreviousToken(false);
            if (className == (string)compiler.PreviousToken.Literal)
                throw new CompilerException("A class cannot inher from itself.");

            compiler.BeginScope();
            compiler.AddLocal(compState, "super");
            compiler.DefineVariable(0);

            compiler.NamedVariable(className, false);
            compiler.EmitOpCode(OpCode.INHERIT);
            hasSuper = true;
            return hasSuper;
        }

        private void DoClassMixins(CompilerBase compiler)
        {
            do
            {
                compiler.Consume(TokenType.IDENTIFIER, "Expect identifier after mixin into class.");
                mixinNames.Push(compiler.PreviousToken.Literal as string);
            } while (compiler.Match(TokenType.COMMA));

            //TODO all of these methods trail with end statement, DRY.
            compiler.Consume(TokenType.END_STATEMENT, "Expect ; after mixin declaration.");
        }

        private void DoInit(CompilerBase compiler)
        {
            byte constant = compiler.AddCustomStringConstant(InitMethodName.String);
            CreateInitMethod(compiler);
            compiler.EmitOpAndBytes(OpCode.METHOD, constant);
        }

        private void CreateInitMethod(CompilerBase compiler)
        {
            compiler.Function(InitMethodName.String, FunctionType.Init);
        }

        private void DoClassMethod(CompilerBase compiler)
        {
            compiler.Consume(TokenType.IDENTIFIER, "Expect method name.");
            byte constant = compiler.AddStringConstant();

            var name = compiler.CurrentChunk.ReadConstant(constant).val.asString.String;
            FunctionType funcType = FunctionType.Method;
            compiler.Function(name, funcType);
            compiler.EmitOpAndBytes(OpCode.METHOD, constant);
        }

        private void DoClassStatic(CompilerBase compiler)
        {
            if (compiler.Match(TokenType.VAR))
            {
                StaticProperty(compiler);
            }
            else
            {
                StaticMethod(compiler);
            }
        }

        private void StaticMethod(CompilerBase compiler)
        {
            compiler.Consume(TokenType.IDENTIFIER, "Expect method name.");
            byte constant = compiler.AddStringConstant();

            var name = compiler.CurrentChunk.ReadConstant(constant).val.asString;

            compiler.Function(name.String, FunctionType.Function);
            compiler.EmitOpAndBytes(OpCode.METHOD, constant);
        }

        private void Property(CompilerBase compiler)
        {
            do
            {
                compiler.Consume(TokenType.IDENTIFIER, "Expect var name.");
                byte nameConstant = compiler.AddStringConstant();

                classVarNames.Add(compiler.CurrentChunk.ReadConstant(nameConstant).val.asString.String);

                var compState = compiler.CurrentCompilerState;

                int initFragmentJump = -1;
                //emit jump // to skip this during imperative
                initFragmentJump = compiler.EmitJump(OpCode.JUMP);
                //patch jump previous init fragment if it exists
                if (_previousInitFragJumpLocation != -1)
                {
                    compiler.PatchJump(_previousInitFragJumpLocation);
                }
                else
                {
                    _initFragStartLocation = compiler.CurrentChunk.Instructions.Count;
                }

                compiler.EmitOpAndBytes(OpCode.GET_LOCAL, 0);//get class or inst this on the stack

                //if = consume it and then
                //eat 1 expression or a push null
                if (compiler.Match(TokenType.ASSIGN))
                {
                    compiler.Expression();
                }
                else
                {
                    compiler.EmitOpCode(OpCode.NULL);
                }

                //emit set prop
                compiler.EmitOpAndBytes(OpCode.SET_PROPERTY, nameConstant);
                compiler.EmitOpCode(OpCode.POP);
                //emit jump // to move to next prop init fragment, defaults to jump nowhere return
                _previousInitFragJumpLocation = compiler.EmitJump(OpCode.JUMP);

                //patch jump from skip imperative
                compiler.PatchJump(initFragmentJump);
            } while (compiler.Match(TokenType.COMMA));

            compiler.Consume(TokenType.END_STATEMENT, "Expect ; after property declaration.");
        }

        private void StaticProperty(CompilerBase compiler)
        {
            do
            {
                compiler.Consume(TokenType.IDENTIFIER, "Expect var name.");
                byte nameConstant = compiler.AddStringConstant();

                compiler.EmitOpAndBytes(OpCode.GET_LOCAL, 1);//get class or inst this on the stack

                //if = consume it and then
                //eat 1 expression or a push null
                if (compiler.Match(TokenType.ASSIGN))
                {
                    compiler.Expression();
                }
                else
                {
                    compiler.EmitOpCode(OpCode.NULL);
                }

                //emit set prop
                compiler.EmitOpAndBytes(OpCode.SET_PROPERTY, nameConstant);
                compiler.EmitOpCode(OpCode.POP);
            } while (compiler.Match(TokenType.COMMA));

            compiler.Consume(TokenType.END_STATEMENT, "Expect ; after property declaration.");
        }
    }
}
