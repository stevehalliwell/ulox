namespace ULox
{
    public class ParseRule
    {
        public System.Action<CompilerBase, bool> Prefix { get; private set; }
        public System.Action<CompilerBase, bool> Infix { get; private set; }
        public Precedence Precedence { get; private set; }

        public ParseRule(
            System.Action<CompilerBase, bool> prefix,
            System.Action<CompilerBase, bool> infix,
            Precedence pre)
        {
            Prefix = prefix;
            Infix = infix;
            Precedence = pre;
        }
    }
}
