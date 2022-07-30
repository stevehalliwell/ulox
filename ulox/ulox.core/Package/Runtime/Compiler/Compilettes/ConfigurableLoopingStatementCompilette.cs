using static ULox.CompilerState;

namespace ULox
{
    public abstract class ConfigurableLoopingStatementCompilette : ICompilette
    {
        public TokenType Match { get; private set; }

        protected ConfigurableLoopingStatementCompilette(TokenType match)
        {
            Match = match;
        }

        public void Process(Compiler compiler)
        {
            ConfigurableLoopingStatement(compiler);
        }

        protected virtual void ConfigurableLoopingStatement(Compiler compiler)
        {
            compiler.BeginScope();

            var comp = compiler.CurrentCompilerState;
            int loopStart = compiler.CurrentChunkInstructinCount;
            var loopState = new LoopState();
            comp.LoopStates.Push(loopState);
            loopState.loopContinuePoint = loopStart;

            loopStart = BeginLoop(compiler, loopStart, loopState);

            compiler.Statement();

            compiler.EmitLoop(loopStart);

            PatchLoopExits(compiler, loopState);

            compiler.EmitOpCode(OpCode.POP);

            compiler.EndScope();
        }

        protected abstract int BeginLoop(Compiler compiler, int loopStart, LoopState loopState);

        protected void PatchLoopExits(Compiler compiler, LoopState loopState)
        {
            if (loopState.loopExitPatchLocations.Count == 0)
                throw new CompilerException("Loops must contain an termination.");

            for (int i = 0; i < loopState.loopExitPatchLocations.Count; i++)
            {
                compiler.PatchJump(loopState.loopExitPatchLocations[i]);
            }
        }
    }
}
