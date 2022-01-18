namespace ULox
{
    public static partial class ScannerBaseExt
    {
        public static void AddSingleCharTokenGenerator(this IScanner scanner, char ch, TokenType tt)
            => scanner.AddGenerator(new ConfiguredSingleCharScannerCharMatchTokenGenerator(ch, tt));

        public static void AddSingleCharTokenGenerators(this IScanner scanner, params (char ch, TokenType token)[] tokens)
        {
            foreach (var item in tokens)
            {
                scanner.AddSingleCharTokenGenerator(item.ch, item.token);
            }
        }

        public static void AddGenerators(this IScanner scanner, params IScannerTokenGenerator[] scannerTokenGenerators)
        {
            foreach (var item in scannerTokenGenerators)
            {
                scanner.AddGenerator(item);
            }
        }
    }
}
