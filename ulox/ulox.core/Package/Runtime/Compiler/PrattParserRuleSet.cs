using System.Linq;

namespace ULox
{
    public class PrattParserRuleSet
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
    }
}
