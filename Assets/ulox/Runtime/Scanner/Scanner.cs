using System.Collections.Generic;

namespace ULox
{
    public class Scanner : ScannerBase
    {
        protected override void Configure()
        {
            defaultGenerators = new List<IScannerTokenGenerator>()
            {
                new WhiteSpaceScannerTokenGenerator(),
                new StringScannerTokenGenerator(),
                new NumberScannerTokenGenerator(),
                new IdentifierScannerTokenGenerator()
            };

            var simpleGeneratorsToAdd = new IScannerCharMatchTokenGenerator[]
            {
                new ConfiguredSingleCharScannerCharMatchTokenGenerator('(', TokenType.OPEN_PAREN),
                new ConfiguredSingleCharScannerCharMatchTokenGenerator(')', TokenType.CLOSE_PAREN),
                new ConfiguredSingleCharScannerCharMatchTokenGenerator('{', TokenType.OPEN_BRACE),
                new ConfiguredSingleCharScannerCharMatchTokenGenerator('}', TokenType.CLOSE_BRACE),
                new ConfiguredSingleCharScannerCharMatchTokenGenerator(',', TokenType.COMMA),
                new ConfiguredSingleCharScannerCharMatchTokenGenerator('.', TokenType.DOT),
                new ConfiguredSingleCharScannerCharMatchTokenGenerator(';', TokenType.END_STATEMENT),
                new ConfiguredSingleCharScannerCharMatchTokenGenerator(':', TokenType.COLON),
                new ConfiguredSingleCharScannerCharMatchTokenGenerator('?', TokenType.QUESTION),

                new CompoundCharScannerCharMatchTokenGenerator('+', TokenType.PLUS, TokenType.PLUS_EQUAL),
                new CompoundCharScannerCharMatchTokenGenerator('-', TokenType.MINUS, TokenType.MINUS_EQUAL),
                new CompoundCharScannerCharMatchTokenGenerator('*', TokenType.STAR, TokenType.STAR_EQUAL),
                new CompoundCharScannerCharMatchTokenGenerator('%', TokenType.PERCENT, TokenType.PERCENT_EQUAL),
                new CompoundCharScannerCharMatchTokenGenerator('!', TokenType.BANG, TokenType.BANG_EQUAL),
                new CompoundCharScannerCharMatchTokenGenerator('=', TokenType.ASSIGN, TokenType.EQUALITY),
                new CompoundCharScannerCharMatchTokenGenerator('<', TokenType.LESS, TokenType.LESS_EQUAL),
                new CompoundCharScannerCharMatchTokenGenerator('>', TokenType.GREATER, TokenType.GREATER_EQUAL),

                new SlashScannerTokenGenerator()
            };

            simpleGenerators = new Dictionary<char, IScannerCharMatchTokenGenerator>();

            foreach (var item in simpleGeneratorsToAdd)
            {
                simpleGenerators[item.MatchingChar] = item;
            }
        }
    }
}
