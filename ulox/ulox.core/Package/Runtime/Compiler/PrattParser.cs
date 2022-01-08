namespace ULox
{
    public class PrattParser
    {
        private readonly IParseRule[] rules;

        public PrattParser()
        {
            rules = new IParseRule[System.Enum.GetNames(typeof(TokenType)).Length];
            var invalidParseRule = new InvalidParseRule();

            for (int i = 0; i < rules.Length; i++)
            {
                rules[i] = invalidParseRule;
            }
        }

        public void SetPrattRule(TokenType tt, IParseRule rule)
           => rules[(int)tt] = rule;

        public IParseRule GetRule(TokenType operatorType) 
            => rules[(int)operatorType];
        
    }
}
