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
            var loopState = new LoopState(compiler.UniqueChunkStringConstant("loop_exit"));
            comp.LoopStates.Push(loopState);

            PreLoop(compiler, loopState);
            
            loopState.StartLabelID = compiler.LabelUniqueChunkLabel("loop_start");
            loopState.ContinueLabelID = loopState.StartLabelID;

            BeginLoop(compiler, loopState);

            compiler.Statement();

            compiler.EmitGoto(loopState.StartLabelID);

            PatchLoopExits(compiler, loopState);

            compiler.EmitOpCode(OpCode.POP);

            compiler.EndScope();
        }

        protected abstract void PreLoop(Compiler compiler, LoopState loopState);

        protected abstract void BeginLoop(Compiler compiler, LoopState loopState);

        protected void PatchLoopExits(Compiler compiler, LoopState loopState)
        {
            if (loopState.loopExitPatchLocations.Count == 0
                && !loopState.HasExit)
                compiler.ThrowCompilerException("Loops must contain a termination");

            for (int i = 0; i < loopState.loopExitPatchLocations.Count; i++)
            {
                compiler.PatchJump(loopState.loopExitPatchLocations[i]);
            }

            compiler.EmitLabel(loopState.ExitLabelID);
        }
    }
}
