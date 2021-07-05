namespace ULox
{
    public class ParseRule
    {
        public System.Action<bool> Prefix { get; private set; }
        public System.Action<bool> Infix { get; private set; }
        public Precedence Precedence { get; private set; }

        public ParseRule(
            System.Action<bool> prefix,
            System.Action<bool> infix,
            Precedence pre)
        {
            Prefix = prefix;
            Infix = infix;
            Precedence = pre;
        }
    }
}
