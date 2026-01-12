namespace ULox
{
    //todo: move this to where it is used
    public class InvalidParseRule : IParseRule
    {
        public Precedence Precedence => Precedence.None;

        public void Infix(Compiler compiler, bool canAssign) 
            => throw new System.NotImplementedException();
        
        public void Prefix(Compiler compiler, bool canAssign) 
            => throw new System.NotImplementedException();
    }
}
