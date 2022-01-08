namespace ULox
{
    public class WhiteSpaceScannerTokenGenerator : IScannerTokenGenerator
    {
        public void Consume(IScanner scanner)
        {
            switch (scanner.CurrentChar)
            {
            case ' ':
            case '\r':
            case '\t':
                //skiping over whitespace
                scanner.CharacterNumber++;
                break;

            case '\n':
                scanner.Line++;
                scanner.CharacterNumber = 0;
                break;
            }
        }

        public bool DoesMatchChar(char ch)
        {
            switch (ch)
            {
                case ' ':
                case '\r':
                case '\t':
                case '\n':
                return true;
            }

            return false;
        }
    }
}
