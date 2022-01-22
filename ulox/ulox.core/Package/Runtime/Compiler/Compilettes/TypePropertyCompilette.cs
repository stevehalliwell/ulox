﻿using System.Collections.Generic;

namespace ULox
{
    public class TypePropertyCompilette : ITypeBodyCompilette
    {
        private List<string> _classVarNames = new List<string>();

        private int _initFragStartLocation = -1;
        private int _previousInitFragJumpLocation = -1;
        private TypeCompilette _typeCompilette;

        public TypePropertyCompilette(TypeCompilette typeCompilette)
        {
            _typeCompilette = typeCompilette;
        }

        public TokenType Match
            => TokenType.VAR;

        public TypeCompiletteStage Stage
            => TypeCompiletteStage.Var;

        public void End() { }

        public void PostBody(Compiler compiler)
        {
            //emit return //if we are the last link in the chain this ends our call

            if (_initFragStartLocation != -1)
                compiler.WriteUShortAt(_typeCompilette.InitChainInstruction, (ushort)_initFragStartLocation);

            //return stub used by init and test chains
            var classReturnEnd = compiler.EmitJump(OpCode.JUMP);

            if (_previousInitFragJumpLocation != -1)
                compiler.PatchJump(_previousInitFragJumpLocation);

            compiler.EmitOpAndBytes(OpCode.RETURN, (byte)ReturnMode.One);

            compiler.PatchJump(classReturnEnd);
        }

        public void PreBody(Compiler compiler) { }

        public void Process(Compiler compiler)
        {
            do
            {
                compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect var name.");
                byte nameConstant = compiler.AddStringConstant();

                _classVarNames.Add(compiler.CurrentChunk.ReadConstant(nameConstant).val.asString.String);

                //emit jump // to skip this during imperative
                int initFragmentJump = compiler.EmitJump(OpCode.JUMP);
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
                if (compiler.TokenIterator.Match(TokenType.ASSIGN))
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
            } while (compiler.TokenIterator.Match(TokenType.COMMA));

            compiler.ConsumeEndStatement("property declaration");
        }

        public void Start()
        {
            _initFragStartLocation = -1;
            _previousInitFragJumpLocation = -1;
            _classVarNames.Clear();
        }
    }
}
