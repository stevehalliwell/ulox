namespace ULox
{
    public interface ICompilette
    {
        TokenType MatchingToken { get; }

        void Process(Compiler compiler);
    }
}
