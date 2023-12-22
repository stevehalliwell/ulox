namespace ULox
{
    public static class CompilerExpressions
    {
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
            Compiler.InnerFunctionDeclaration(compiler, false);
        }

        //todo can this be sugar?
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

        //todo can be sugar?
        public static void BraceCreateDynamic(Compiler compiler, bool arg2)
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

        public static void FName(Compiler compiler, bool canAssign)
        {
            var fname = compiler.CurrentChunk.Name;
            compiler.AddConstantAndWriteOp(Value.New(fname));
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
    }
}
