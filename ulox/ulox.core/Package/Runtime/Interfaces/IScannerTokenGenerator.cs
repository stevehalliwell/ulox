namespace ULox
{
    public interface IScannerTokenGenerator
    {
        bool DoesMatchChar(char ch);

        void Consume(IScanner scanner);
    }
}
