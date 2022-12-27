namespace ULox
{
    public sealed class WhiteSpaceScannerTokenGenerator : IScannerTokenGenerator
    {
        public void Consume(Scanner scanner)
        {

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
