namespace ULox
{
    public interface ICompilette
    {
        TokenType Match { get; }

        void Process();
    }
}