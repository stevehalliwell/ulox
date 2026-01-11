using System.Linq;

namespace ULox
{
    public enum Precedence
    {
        None,
        Assignment,
        Or,
        And,
        Equality,
        Comparison,
        Term,
        Factor,
        Unary,
        Call,
        Primary,
    }

    public interface IParseRule
    {
        Precedence Precedence { get; }
        void Prefix(Compiler compiler, bool canAssign);
        void Infix(Compiler compiler, bool canAssign);
    }
    
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
            var rule = GetRule(compiler.TokenIterator.PreviousToken.TokenType);

            var canAssign = pre <= Precedence.Assignment;
            rule.Prefix(compiler, canAssign);

            while (pre <= (rule = GetRule(compiler.TokenIterator.CurrentToken.TokenType)).Precedence)
            {
                compiler.TokenIterator.Advance();
                rule.Infix(compiler, canAssign);
            }

            if (canAssign && compiler.TokenIterator.Match(TokenType.ASSIGN))
                compiler.ThrowCompilerException("Invalid assignment target");
        }
    }
}
