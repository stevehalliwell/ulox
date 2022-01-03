using System;

namespace ULox
{
    public class CompiletteAction : ICompilette
    {
        private readonly Action<CompilerBase> processAction;

        public CompiletteAction(
            TokenType match,
            Action<CompilerBase> processAction)
        {
            this.processAction = processAction;
            Match = match;
        }

        public TokenType Match { get; private set; }

        public void Process(CompilerBase compiler)
            => processAction.Invoke(compiler);
    }
}
