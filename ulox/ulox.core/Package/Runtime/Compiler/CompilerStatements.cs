using System.Linq;
using static ULox.CompilerState;

namespace ULox
{
    public static class CompilerStatements
    {
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

                compiler.EmitLabel(elseJump);
            }
            else
            {
                compiler.EmitLabel(wasFalseLabel);
                compiler.EmitPop();
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

        public static void NoOpStatement(Compiler compiler)
        {
        }

        public static void FreezeStatement(Compiler compiler)
        {
            compiler.Expression();
            compiler.EmitPacket(new ByteCodePacket(OpCode.FREEZE));
            compiler.ConsumeEndStatement();
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
                compiler.EmitPop(1);
               
                compiler.AddConstantAndWriteOp(Value.New("Expect failed, '"));

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
               
                compiler.AddConstantAndWriteOp(Value.New("'"));
                compiler.EmitPacket(new ByteCodePacket(OpCode.ADD));
                compiler.EmitPacket(new ByteCodePacket(OpCode.ADD));
                compiler.EmitPacket(new ByteCodePacket(OpCode.THROW));
                compiler.EmitLabel(thenjumpLabel);
                compiler.EmitPop(1);
            }
            while (compiler.TokenIterator.Match(TokenType.COMMA));

            compiler.ConsumeEndStatement();
        }

        //todo match be sugar?
        //could become if (a) statement elseif (b) statement // else throw $"Match on '{matchArgName}' did have a matching case."
        public static void MatchStatement(Compiler compiler)
        {
            //make a scope
            compiler.BeginScope();

            compiler.TokenIterator.Consume(TokenType.IDENTIFIER, "Expect identifier after match statement.");
            var matchArgName = compiler.TokenIterator.PreviousToken.Lexeme;
            var resolveRes = compiler.ResolveNameLookupOpCode(matchArgName);

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
                compiler.EmitPacketFromResolveGet(resolveRes);
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
            var loopState = new LoopState(compiler.UniqueChunkLabelStringConstant("loop_exit"));
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
                var newStartLabel = compiler.LabelUniqueChunkLabel("loop_start");
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
