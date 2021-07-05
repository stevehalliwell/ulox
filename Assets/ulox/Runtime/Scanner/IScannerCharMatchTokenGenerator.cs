namespace ULox
{
    public interface IScannerCharMatchTokenGenerator
    {
        char MatchingChar { get; }
        void Consume(ScannerBase scanner);
    }
}