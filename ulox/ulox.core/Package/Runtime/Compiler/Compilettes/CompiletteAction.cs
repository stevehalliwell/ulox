using System;

namespace ULox
{
    public class CompiletteAction : ICompilette
    {
        private readonly Action<Compiler> processAction;

        public CompiletteAction(
            TokenType match,
            Action<Compiler> processAction)
        {
            this.processAction = processAction;
            Match = match;
        }

        public TokenType Match { get; }

        public void Process(Compiler compiler)
            => processAction.Invoke(compiler);
    }
}
