using System.Collections.Generic;
using System.Linq;

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
                new IdentifierScannerTokenGenerator(IdentifierTokenTypeTuple())
            };

            var simpleGeneratorsToAdd = new List<IScannerCharMatchTokenGenerator>();

            simpleGeneratorsToAdd.AddRange(
                CharToTokenTuple()
                .Select(x => new ConfiguredSingleCharScannerCharMatchTokenGenerator(x.ch, x.token))
                .Select(x => (IScannerCharMatchTokenGenerator)x));

            simpleGeneratorsToAdd.AddRange(
                CharToCompoundTokenTuple()
                .Select(x => new CompoundCharScannerCharMatchTokenGenerator(x.ch, x.tokenFlat, x.tokenCompound))
                .Select(x => (IScannerCharMatchTokenGenerator)x));

            simpleGeneratorsToAdd.Add(new SlashScannerTokenGenerator());

            simpleGenerators = new Dictionary<char, IScannerCharMatchTokenGenerator>();

            foreach (var item in simpleGeneratorsToAdd)
            {
                simpleGenerators[item.MatchingChar] = item;
            }
        }

        protected virtual (char ch, TokenType token)[] CharToTokenTuple()
        {
            return new[]
            {
                ('(', TokenType.OPEN_PAREN),
                (')', TokenType.CLOSE_PAREN),
                ('{', TokenType.OPEN_BRACE),
                ('}', TokenType.CLOSE_BRACE),
                (',', TokenType.COMMA),
                ('.', TokenType.DOT),
                (';', TokenType.END_STATEMENT),
                (':', TokenType.COLON),
                ('?', TokenType.QUESTION),
            };
        }

        protected virtual (char ch, TokenType tokenFlat, TokenType tokenCompound)[] CharToCompoundTokenTuple()
        {
            return new[]
            {
                ('+', TokenType.PLUS, TokenType.PLUS_EQUAL),
                ('-', TokenType.MINUS, TokenType.MINUS_EQUAL),
                ('*', TokenType.STAR, TokenType.STAR_EQUAL),
                ('%', TokenType.PERCENT, TokenType.PERCENT_EQUAL),
                ('!', TokenType.BANG, TokenType.BANG_EQUAL),
                ('=', TokenType.ASSIGN, TokenType.EQUALITY),
                ('<', TokenType.LESS, TokenType.LESS_EQUAL),
                ('>', TokenType.GREATER, TokenType.GREATER_EQUAL),
            };
        }

        protected virtual (string, TokenType)[] IdentifierTokenTypeTuple()
        {
            return new[]
            {
                ( "var",    TokenType.VAR),
                ( "string", TokenType.STRING),
                ( "int",    TokenType.INT),
                ( "float",  TokenType.FLOAT),
                ( "and",    TokenType.AND),
                ( "or",     TokenType.OR),
                ( "if",     TokenType.IF),
                ( "else",   TokenType.ELSE),
                ( "while",  TokenType.WHILE),
                ( "for",    TokenType.FOR),
                ( "loop",   TokenType.LOOP),
                ( "return", TokenType.RETURN),
                ( "break",  TokenType.BREAK),
                ( "continue", TokenType.CONTINUE),
                ( "true",   TokenType.TRUE),
                ( "false",  TokenType.FALSE),
                ( "null",   TokenType.NULL),
                ( "fun",    TokenType.FUNCTION),
                ( "class",  TokenType.CLASS),
                ( "this",  TokenType.THIS),
                ( "super",  TokenType.SUPER),
                ( ".",      TokenType.DOT),
                ( "throw",  TokenType.THROW),
                ( "test",  TokenType.TEST),
                ( "testcase",  TokenType.TESTCASE),
                ( "static",  TokenType.STATIC),
                ( "yield",  TokenType.YIELD),
                ( "init",  TokenType.INIT),
            };
        }
    }
}
