namespace ULox
{
    public class ActionParseRule : IParseRule
    {
        public System.Action<Compiler, bool> PrefixAction { get; private set; }
        public System.Action<Compiler, bool> InfixAction { get; private set; }
        public Precedence Precedence { get; private set; }

        public ActionParseRule(
            System.Action<Compiler, bool> prefix,
            System.Action<Compiler, bool> infix,
            Precedence pre)
        {
            PrefixAction = prefix;
            InfixAction = infix;
            Precedence = pre;
        }

        public void Prefix(Compiler compiler, bool canAssign)
            => PrefixAction.Invoke(compiler, canAssign);

        public void Infix(Compiler compiler, bool canAssign)
            => InfixAction.Invoke(compiler, canAssign);
    }
}
