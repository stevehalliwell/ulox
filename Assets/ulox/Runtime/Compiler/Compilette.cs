using System;

namespace ULox
{
    public class Compilette : ICompilette
    {
        private readonly Action<CompilerBase> processAction;

        public Compilette(
            TokenType match,
            Action<CompilerBase> processAction)
        {
            this.processAction = processAction;
            Match = match;
        }

        public TokenType Match { get; private set; }
        public void Process(CompilerBase compiler)
        {
            processAction.Invoke(compiler);
        }
    }
}
