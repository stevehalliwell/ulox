namespace ULox
{
    public class WhiteSpaceScannerTokenGenerator : IScannerTokenGenerator
    {
        public void Consume(ScannerBase scanner) { }

        public bool DoesMatchChar(ScannerBase scanner)
        {
            switch (scanner.CurrentChar)
            {
            case ' ':
            case '\r':
            case '\t':
                //skiping over whitespace
                scanner.CharacterNumber++;
                return true;

            case '\n':
                scanner.Line++;
                scanner.CharacterNumber = 0;
                return true;
            }

            return false;
        }
    }
}
