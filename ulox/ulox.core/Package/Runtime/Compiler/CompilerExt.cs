namespace ULox
{
    public static class CompilerExt
    {
        public static void AddDeclarationCompilette(this Compiler comp, params ICompilette[] compilettes)
        {
            foreach (var item in compilettes)
            {
                comp.AddDeclarationCompilette(item);
            }
        }

        public static void AddStatementCompilette(this Compiler comp, params (TokenType match, System.Action<Compiler> action)[] processActions)
        {
            foreach (var item in processActions)
            {
                comp.AddStatementCompilette(new CompiletteAction(item.match, item.action));
            }
        }

        public static void AddStatementCompilette(this Compiler comp, params ICompilette[] compilettes)
        {
            foreach (var item in compilettes)
            {
                comp.AddStatementCompilette(item);
            }
        }

        public static void SetPrattRules(this Compiler comp, params (TokenType tt, IParseRule rule)[] rules)
        {
            foreach (var (tt, rule) in rules)
            {
                comp.SetPrattRule(tt, rule);
            }
        }
    }
}
