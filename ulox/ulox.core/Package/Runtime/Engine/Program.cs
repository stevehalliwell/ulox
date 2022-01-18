namespace ULox
{
    public class Program : ProgramBase<Compiler, Disassembler>
    {
        public Program()
            : base(ScannerFactory.CreateScanner())
        {
        }
    }
}
