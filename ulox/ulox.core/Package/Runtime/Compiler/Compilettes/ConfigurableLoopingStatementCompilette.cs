using static ULox.CompilerState;

namespace ULox
{
    public abstract class ConfigurableLoopingStatementCompilette : ICompilette
    {
        public TokenType MatchingToken { get; }

        protected ConfigurableLoopingStatementCompilette(TokenType match)
        {
            MatchingToken = match;
        }

        public void Process(Compiler compiler)
        {
            ConfigurableLoopingStatement(compiler);
        }

        protected virtual void ConfigurableLoopingStatement(Compiler compiler)
        {
            compiler.BeginScope();

            var comp = compiler.CurrentCompilerState;
            var loopState = new LoopState(compiler.UniqueChunkLabelStringConstant("loop_exit"));
            comp.LoopStates.Push(loopState);

            PreLoop(compiler, loopState);
            
            loopState.StartLabelID = compiler.LabelUniqueChunkLabel("loop_start");
            loopState.ContinueLabelID = loopState.StartLabelID;

            BeginLoop(compiler, loopState);

            compiler.Statement();

            compiler.EmitGoto(loopState.StartLabelID);

            if (!loopState.HasExit)
                compiler.ThrowCompilerException("Loops must contain a termination");
            compiler.EmitLabel(loopState.ExitLabelID);
            compiler.EmitPop();

            compiler.EndScope();
        }

        protected abstract void PreLoop(Compiler compiler, LoopState loopState);

        protected abstract void BeginLoop(Compiler compiler, LoopState loopState);
    }
}
