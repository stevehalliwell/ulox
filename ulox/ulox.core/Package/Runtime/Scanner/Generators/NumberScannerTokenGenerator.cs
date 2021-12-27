using System.Text;

namespace ULox
{
    public class NumberScannerTokenGenerator : IScannerTokenGenerator
    {
        private StringBuilder workingSpaceStringBuilder = new StringBuilder();

        public static bool IsDigit(int ch) => ch >= '0' && ch <= '9';

        public bool DoesMatchChar(ScannerBase scanner) => IsDigit(scanner.CurrentChar);

        public void Consume(ScannerBase scanner)
        {
            bool hasFoundDecimalPoint = false;
            workingSpaceStringBuilder.Clear();

            workingSpaceStringBuilder.Append(scanner.CurrentChar);

            while (IsDigit(scanner.Peek()))
            {
                scanner.Advance();
                workingSpaceStringBuilder.Append(scanner.CurrentChar);
            }

            if (scanner.Peek() == '.')
            {
                scanner.Advance();
                workingSpaceStringBuilder.Append(scanner.CurrentChar);
                hasFoundDecimalPoint = true;
                while (IsDigit(scanner.Peek()))
                {
                    scanner.Advance();
                    workingSpaceStringBuilder.Append(scanner.CurrentChar);
                }
            }

            var numStr = workingSpaceStringBuilder.ToString();

            scanner.AddToken(hasFoundDecimalPoint ? TokenType.FLOAT : TokenType.INT,
                numStr,
                double.Parse(numStr));
        }
    }
}
