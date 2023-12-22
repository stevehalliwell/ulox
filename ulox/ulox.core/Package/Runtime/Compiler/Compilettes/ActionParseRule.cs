using System;

namespace ULox
{
    public class ActionParseRule : IParseRule
    {
        public System.Action<Compiler, bool> PrefixAction { get; }
        public System.Action<Compiler, bool> InfixAction { get; }
        public Precedence Precedence { get; }

        public ActionParseRule(
            Action<Compiler, bool> prefix,
            Action<Compiler, bool> infix,
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
