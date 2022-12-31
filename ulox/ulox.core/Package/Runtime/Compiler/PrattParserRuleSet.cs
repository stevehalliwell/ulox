using System.Linq;

namespace ULox
{
    public sealed class PrattParserRuleSet
    {
        private readonly IParseRule[] rules;

        public PrattParserRuleSet()
        {
            var count = System.Enum.GetNames(typeof(TokenType)).Length;
            IParseRule invalidParseRule = new InvalidParseRule();
            rules = Enumerable.Repeat(invalidParseRule, count).ToArray();
        }

        public void SetPrattRule(TokenType tt, IParseRule rule)
           => rules[(int)tt] = rule;

        public IParseRule GetRule(TokenType operatorType) 
            => rules[(int)operatorType];

        public void ParsePrecedence(Compiler compiler, Precedence pre)
        {
            compiler.TokenIterator.Advance();
            var rule = GetRule(compiler.PreviousTokenType);

            var canAssign = pre <= Precedence.Assignment;
            rule.Prefix(compiler, canAssign);

            while (pre <= GetRule(compiler.CurrentTokenType).Precedence)
            {
                compiler.TokenIterator.Advance();
                rule = GetRule(compiler.PreviousTokenType);
                rule.Infix(compiler, canAssign);
            }

            if (canAssign && compiler.TokenIterator.Match(TokenType.ASSIGN))
                compiler.ThrowCompilerException("Invalid assignment target");
        }
    }
}
