using System.Collections.Generic;
using System.Linq;

namespace ULox
{
    public class SoaClassDesugar : IDesugarStep
    {
        public DesugarStepRequest IsDesugarRequested(TokenIterator tokenIterator, ICompilerDesugarContext context)
        {
            if (tokenIterator.CurrentToken.TokenType == TokenType.SOA
               && tokenIterator.PeekType(1) == TokenType.IDENTIFIER
               && tokenIterator.PeekType(2) == TokenType.OPEN_BRACE)
                return DesugarStepRequest.Replace;

            return DesugarStepRequest.None;
        }

        public void ProcessDesugar(int currentTokenIndex, List<Token> tokens, ICompilerDesugarContext context)
        {
            var endOfBlock = TokenIterator.FindClosing(tokens, currentTokenIndex + 3, TokenType.OPEN_BRACE, TokenType.CLOSE_BRACE);

            var soaTokens = tokens.GetRange(currentTokenIndex, endOfBlock - currentTokenIndex + 1);
            //remove those
            tokens.RemoveRange(currentTokenIndex, endOfBlock - currentTokenIndex + 1);

            var typeInfo = context.TypeInfo;

            var soaTypes = soaTokens
                .Where(x => x.TokenType == TokenType.IDENTIFIER)
                .SelectMany(x => typeInfo.Types.Where(y => y.Name == x.Lexeme))
                .ToList();

            var prototypeToken = soaTokens[2];

            var toInsert = new List<Token>
            {
                soaTokens[0].MutateType(TokenType.CLASS),
                soaTokens[1],
                soaTokens[2],
                prototypeToken.MutateType(TokenType.VAR),
            };

            toInsert.AddRange(soaTypes.SelectMany(type => type.Fields.SelectMany(x => new[]
            {
                prototypeToken.Mutate(TokenType.IDENTIFIER, x, x),
                prototypeToken.MutateType(TokenType.ASSIGN),
                prototypeToken.MutateType(TokenType.OPEN_BRACKET),
                prototypeToken.MutateType(TokenType.CLOSE_BRACKET),
                prototypeToken.MutateType(TokenType.COMMA),
            })));
            toInsert.AddRange(new[]
            {
                soaTokens.Last().MutateType(TokenType.END_STATEMENT),
            });

            AppendCountMethod(prototypeToken, soaTypes, toInsert);
            AppendClearMethod(prototypeToken, soaTypes, toInsert);
            AppendAddMethod(prototypeToken, soaTypes, toInsert);
            AppendRemoveAtMethod(prototypeToken, soaTypes, toInsert);

            //end class
            toInsert.AddRange(new[]
            {
                soaTokens.Last(),
            });

            //add new class
            tokens.InsertRange(currentTokenIndex, toInsert);
        }

        private void AppendRemoveAtMethod(Token prototypeToken, List<TypeInfoEntry> soaTypes, List<Token> toInsert)
        {
            var indexToken = prototypeToken.Mutate(TokenType.IDENTIFIER, "index", "index");
            toInsert.AddRange(new[]
            {
                prototypeToken.Mutate(TokenType.IDENTIFIER, "RemoveAt", "RemoveAt"),
                prototypeToken.MutateType(TokenType.OPEN_PAREN),
                indexToken,
                prototypeToken.MutateType(TokenType.CLOSE_PAREN),
                prototypeToken.MutateType(TokenType.OPEN_BRACE),
            });

            toInsert.AddRange(soaTypes
                .SelectMany(x => x.Fields.SelectMany(field => new[]
                {
                    prototypeToken.Mutate(TokenType.IDENTIFIER, field, field),
                    prototypeToken.MutateType(TokenType.DOT),
                    prototypeToken.Mutate(TokenType.IDENTIFIER, "RemoveAt", "RemoveAt"),
                    prototypeToken.MutateType(TokenType.OPEN_PAREN),
                    indexToken,
                    prototypeToken.MutateType(TokenType.CLOSE_PAREN),
                    prototypeToken.MutateType(TokenType.END_STATEMENT),
                })));

            toInsert.AddRange(new[]
            {
                prototypeToken.MutateType(TokenType.CLOSE_BRACE),
            });
        }

        private void AppendAddMethod(Token prototypeToken, List<TypeInfoEntry> soaTypes, List<Token> toInsert)
        {
            var soaTypesNames = soaTypes.Select(x => (x.Name.ToLower() + "Item", x)).ToList();

            toInsert.AddRange(new[]
            {
                prototypeToken.Mutate(TokenType.IDENTIFIER, "Add", "Add"),
                prototypeToken.MutateType(TokenType.OPEN_PAREN),
            });
            var parameters = soaTypesNames
                .SelectMany(x => new[]
                {
                    prototypeToken.MutateType(TokenType.COMMA),
                    prototypeToken.Mutate(TokenType.IDENTIFIER, x.Item1, x.Item1),
                })
                .Skip(1);
            toInsert.AddRange(parameters);
            toInsert.AddRange(new[]
            {
                prototypeToken.MutateType(TokenType.CLOSE_PAREN),
                prototypeToken.MutateType(TokenType.OPEN_BRACE),
            });

            toInsert.AddRange(soaTypesNames
                .SelectMany(x => x.x.Fields.SelectMany(field => new[]
                {
                    prototypeToken.Mutate(TokenType.IDENTIFIER, field, field),
                    prototypeToken.MutateType(TokenType.DOT),
                    prototypeToken.Mutate(TokenType.IDENTIFIER, "Add", "Add"),
                    prototypeToken.MutateType(TokenType.OPEN_PAREN),
                    prototypeToken.Mutate(TokenType.IDENTIFIER, x.Item1, x.Item1),
                    prototypeToken.MutateType(TokenType.DOT),
                    prototypeToken.Mutate(TokenType.IDENTIFIER, field, field),
                    prototypeToken.MutateType(TokenType.CLOSE_PAREN),
                    prototypeToken.MutateType(TokenType.END_STATEMENT),
                })));

            toInsert.AddRange(new[]
            {
                prototypeToken.MutateType(TokenType.CLOSE_BRACE),
            });
        }

        private void AppendClearMethod(Token prototypeToken, List<TypeInfoEntry> soaTypes, List<Token> toInsert)
        {
            toInsert.AddRange(new[]
            {
                prototypeToken.Mutate(TokenType.IDENTIFIER, "Clear", "Clear"),
                prototypeToken.MutateType(TokenType.OPEN_PAREN),
                prototypeToken.MutateType(TokenType.CLOSE_PAREN),
                prototypeToken.MutateType(TokenType.OPEN_BRACE),
            });

            toInsert.AddRange(soaTypes
                .SelectMany(x => x.Fields.SelectMany(field => new[]
                {
                    prototypeToken.Mutate(TokenType.IDENTIFIER, field, field),
                    prototypeToken.MutateType(TokenType.DOT),
                    prototypeToken.Mutate(TokenType.IDENTIFIER, "Clear", "Clear"),
                    prototypeToken.MutateType(TokenType.OPEN_PAREN),
                    prototypeToken.MutateType(TokenType.CLOSE_PAREN),
                    prototypeToken.MutateType(TokenType.END_STATEMENT),
                })));

            toInsert.AddRange(new[]
            {
                prototypeToken.MutateType(TokenType.CLOSE_BRACE),
            });
        }

        private static void AppendCountMethod(Token prototypeToken, List<TypeInfoEntry> soaTypes, List<Token> toInsert)
        {
            var firstName = soaTypes.First().Fields.First();
            toInsert.AddRange(new[]
            {
                prototypeToken.Mutate(TokenType.IDENTIFIER, "Count", "Count"),
                prototypeToken.MutateType(TokenType.OPEN_PAREN),
                prototypeToken.MutateType(TokenType.CLOSE_PAREN),
                prototypeToken.MutateType(TokenType.OPEN_BRACE),
                prototypeToken.Mutate(TokenType.IDENTIFIER, "retval", "retval"),
                prototypeToken.MutateType(TokenType.ASSIGN),
                prototypeToken.Mutate(TokenType.IDENTIFIER, firstName, firstName),
                prototypeToken.MutateType(TokenType.DOT),
                prototypeToken.Mutate(TokenType.IDENTIFIER, "Count", "Count"),
                prototypeToken.MutateType(TokenType.OPEN_PAREN),
                prototypeToken.MutateType(TokenType.CLOSE_PAREN),
                prototypeToken.MutateType(TokenType.END_STATEMENT),
                prototypeToken.MutateType(TokenType.CLOSE_BRACE),
            });
        }
    }
}
