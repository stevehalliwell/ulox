namespace ULox
{
    public interface IParseRule
    {
        Precedence Precedence { get; }
        void Prefix(Compiler compiler, bool canAssign);
        void Infix(Compiler compiler, bool canAssign);
    }
}
