using System;

namespace ULox
{
    public class Compilette : ICompilette
    {
        private readonly Action processAction;

        public Compilette(
            TokenType match,
            Action processAction)
        {
            this.processAction = processAction;
            Match = match;
        }

        public TokenType Match { get; private set; }
        public void Process()
        {
            processAction.Invoke();
        }
    }
}
