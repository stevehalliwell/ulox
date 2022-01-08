namespace ULox
{
    public interface IParseRule
    {
        Precedence Precedence { get; }
        void Prefix(CompilerBase compiler, bool canAssign);
        void Infix(CompilerBase compiler, bool canAssign);
    }
}
