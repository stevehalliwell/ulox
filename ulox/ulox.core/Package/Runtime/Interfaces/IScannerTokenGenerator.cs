namespace ULox
{
    public interface IScannerTokenGenerator
    {
        bool DoesMatchChar(char ch);

        Token Consume(Scanner scanner);
    }
}
