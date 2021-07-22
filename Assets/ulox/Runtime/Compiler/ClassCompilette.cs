using System.Collections.Generic;

namespace ULox
{
    public class ClassCompilette : ICompilette
    {
        protected Dictionary<TokenType, ICompilette> innerDeclarationCompilettes = new Dictionary<TokenType, ICompilette>();

        public void AddInnerDeclarationCompilette(ICompilette compilette)
            => innerDeclarationCompilettes[compilette.Match] = compilette;

        public TokenType Match => TokenType.CLASS;

        public void Process(CompilerBase compiler)
        {
            ClassDeclaration(compiler);
        }

        private void ClassDeclaration(CompilerBase compiler)
        {
            compiler.Consume(TokenType.IDENTIFIER, "Expect class name.");
            var className = (string)compiler.PreviousToken.Literal;
            var compState = compiler.CurrentCompilerState;
            compState.classCompilerStates.Push(new ClassCompilerState(className));

            byte nameConstant = compiler.AddStringConstant();
            compiler.DeclareVariable();

            compiler.EmitOpAndByte(OpCode.CLASS, nameConstant);
            compiler.DefineVariable(nameConstant);

            bool hasSuper = false;

            if (compiler.Match(TokenType.LESS))
            {
                compiler.Consume(TokenType.IDENTIFIER, "Expect superclass name.");
                compiler.NamedVariableFromPreviousToken(false);
                if (className == (string)compiler.PreviousToken.Literal)
                    throw new CompilerException("A class cannot inhert from itself.");

                compiler.BeginScope();
                compiler.AddLocal(compState, "super");
                compiler.DefineVariable(0);

                compiler.NamedVariable(className, false);
                compiler.EmitOpCode(OpCode.INHERIT);
                hasSuper = true;
            }

            compiler.NamedVariable(className, false);
            compiler.Consume(TokenType.OPEN_BRACE, "Expect '{' before class body.");
            while (!compiler.Check(TokenType.CLOSE_BRACE) && !compiler.Check(TokenType.EOF))
            {
                if (compiler.Match(TokenType.STATIC))
                {
                    if (compiler.Match(TokenType.VAR))
                    {
                        Property(compiler, true);
                    }
                    else
                    {
                        Method(compiler, true);
                    }
                }
                else if (compiler.Match(TokenType.VAR))
                {
                    Property(compiler, false);
                }
                else if (innerDeclarationCompilettes.TryGetValue(compiler.CurrentToken.TokenType, out var complette))
                {
                    compiler.Advance();
                    complette.Process(compiler);
                }
                else
                {
                    Method(compiler, false);
                }
            }

            //emit return //if we are the last link in the chain this ends our call
            var classCompState = compiler.CurrentCompilerState.classCompilerStates.Peek();

            if (classCompState.initFragStartLocation != -1)
            {
                compiler.EmitOpCode(OpCode.INIT_CHAIN_START);
                compiler.EmitUShort((ushort)classCompState.initFragStartLocation);
            }

            if (classCompState.testFragStartLocation != -1)
            {
                compiler.EmitOpCode(OpCode.TEST_CHAIN_START);
                compiler.EmitUShort((ushort)classCompState.testFragStartLocation);
            }

            //return stub used by init and test chains
            var classReturnEnd = compiler.EmitJump(OpCode.JUMP);

            if (classCompState.previousInitFragJumpLocation != -1)
                compiler.PatchJump(classCompState.previousInitFragJumpLocation);

            if (classCompState.previousTestFragJumpLocation != -1)
                compiler.PatchJump(classCompState.previousTestFragJumpLocation);

            //EmitOpCode(OpCode.NULL);
            compiler.EmitOpCode(OpCode.RETURN);

            compiler.PatchJump(classReturnEnd);

            compiler.Consume(TokenType.CLOSE_BRACE, "Expect '}' after class body.");
            compiler.EmitOpCode(OpCode.POP);

            if (hasSuper)
            {
                compiler.EndScope();
            }

            compState.classCompilerStates.Pop();
        }

        private void Method(CompilerBase compiler, bool isStatic)
        {
            compiler.Consume(TokenType.IDENTIFIER, "Expect method name.");
            byte constant = compiler.AddStringConstant();

            var name = compiler.CurrentChunk.ReadConstant(constant).val.asString;
            var funcType = isStatic ? FunctionType.Function : FunctionType.Method;
            if (name == "init")
                funcType = FunctionType.Init;

            compiler.Function(name, funcType);
            compiler.EmitOpAndByte(OpCode.METHOD, constant);
        }

        private void Property(CompilerBase compiler, bool isStatic)
        {
            do
            {
                compiler.Consume(TokenType.IDENTIFIER, "Expect var name.");
                byte nameConstant = compiler.AddStringConstant();

                var compState = compiler.CurrentCompilerState;
                var classCompState = compState.classCompilerStates.Peek();

                int initFragmentJump = -1;
                if (!isStatic)
                {
                    //emit jump // to skip this during imperative
                    initFragmentJump = compiler.EmitJump(OpCode.JUMP);
                    //patch jump previous init fragment if it exists
                    if (classCompState.previousInitFragJumpLocation != -1)
                    {
                        compiler.PatchJump(classCompState.previousInitFragJumpLocation);
                    }
                    else
                    {
                        classCompState.initFragStartLocation = compiler.CurrentChunk.Instructions.Count;
                    }
                }


                compiler.EmitOpAndByte(OpCode.GET_LOCAL, (byte)(isStatic ? 1 : 0));//get class or inst this on the stack


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
                compiler.EmitOpAndByte(OpCode.SET_PROPERTY, nameConstant);
                compiler.EmitOpCode(OpCode.POP);
                if (!isStatic)
                {
                    //emit jump // to move to next prop init fragment, defaults to jump nowhere return
                    classCompState.previousInitFragJumpLocation = compiler.EmitJump(OpCode.JUMP);

                    //patch jump from skip imperative
                    compiler.PatchJump(initFragmentJump);
                }

            } while (compiler.Match(TokenType.COMMA));

            compiler.Consume(TokenType.END_STATEMENT, "Expect ; after property declaration.");
        }
    }
}
