namespace ULox
{
    public class ActionParseRule : IParseRule
    {
        public System.Action<CompilerBase, bool> PrefixAction { get; private set; }
        public System.Action<CompilerBase, bool> InfixAction { get; private set; }
        public Precedence Precedence { get; private set; }

        public ActionParseRule(
            System.Action<CompilerBase, bool> prefix,
            System.Action<CompilerBase, bool> infix,
            Precedence pre)
        {
            PrefixAction = prefix;
            InfixAction = infix;
            Precedence = pre;
        }

        public void Prefix(CompilerBase compiler, bool canAssign)
            => PrefixAction?.Invoke(compiler, canAssign);

        public void Infix(CompilerBase compiler, bool canAssign)
            => InfixAction?.Invoke(compiler, canAssign);
    }
}
