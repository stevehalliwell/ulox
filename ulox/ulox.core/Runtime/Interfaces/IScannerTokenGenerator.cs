namespace ULox
{
    public interface IScannerTokenGenerator
    {
        bool DoesMatchChar(ScannerBase scanner);

        void Consume(ScannerBase scanner);
    }
}
