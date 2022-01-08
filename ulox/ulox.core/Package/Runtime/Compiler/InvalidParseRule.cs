namespace ULox
{
    public class InvalidParseRule : IParseRule
    {
        public Precedence Precedence => Precedence.None;

        public void Infix(CompilerBase compiler, bool canAssign)
        {
            throw new System.NotImplementedException();
        }

        public void Prefix(CompilerBase compiler, bool canAssign)
        {
            throw new System.NotImplementedException();
        }
    }
}
