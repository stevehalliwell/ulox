using System.Collections.Generic;

namespace ULox
{
    public class LoopDesugar : IDesugarStep
    {
        public void ProcessDesugar(int currentTokenIndex, List<Token> tokens, ICompilerDesugarContext context)
        {
            if (tokens[currentTokenIndex + 1].TokenType == TokenType.OPEN_BRACE)
            {
                ProcessReplaceEndlessLoop(currentTokenIndex, tokens);
                return;
            }

            //we expect
            //  `loop arr,i { print(item);}`
            //  and we are going, create 'i'tem, 'i'count and replace with
            //  ` if(arr)
            //    {
            //        var count = arr.Count();
            //        if (count > 0)
            //        {
            //            var i = 0;
            //            var item = arr[i];
            //            for (; i < icount; i += 1)
            //            {
            //                item = arr[i];
            //                print(item);
            //            }
            //        }
            //    }
            var currentToken = tokens[currentTokenIndex];

            var iIdent = "i";
            var toRemove = 2;
            var origIdentTok = tokens[currentTokenIndex + 1];
            var uniqueArrName = context.UniqueLocalName("arr");
            var arrIdent = origIdentTok.Mutate(TokenType.IDENTIFIER, uniqueArrName);

            var openBlockLoc = tokens.FindIndex(currentTokenIndex, x => x.TokenType == TokenType.OPEN_BRACE);
            var nextCommaLoc = tokens.FindIndex(currentTokenIndex, x => x.TokenType == TokenType.COMMA);
            var endArrExpAt = openBlockLoc;
            if (nextCommaLoc != -1 && nextCommaLoc < openBlockLoc)
            {
                //we have more args
                endArrExpAt = nextCommaLoc;
            }

            toRemove = endArrExpAt - currentTokenIndex;

            if (tokens[currentTokenIndex + toRemove].TokenType == TokenType.COMMA)
            {
                iIdent = tokens[currentTokenIndex + toRemove + 1].Literal;
                toRemove += 2;
            }
            var itemIdent = iIdent + "tem";
            var countIdent = iIdent + "count";

            var insertTokens = new List<Token>(64 + endArrExpAt - currentTokenIndex);

            //make local name for arr exp
            insertTokens.Add(currentToken.MutateType(TokenType.OPEN_BRACE));
            insertTokens.Add(currentToken.MutateType(TokenType.VAR));
            insertTokens.Add(arrIdent);
            insertTokens.Add(currentToken.MutateType(TokenType.ASSIGN));
            for (var i = currentTokenIndex + 1; i < endArrExpAt; i++)
                insertTokens.Add(tokens[i]);
            insertTokens.Add(currentToken.MutateType(TokenType.END_STATEMENT));

            //if arr is valid
            insertTokens.Add(currentToken.MutateType(TokenType.IF));
            insertTokens.Add(currentToken.MutateType(TokenType.OPEN_PAREN));
            insertTokens.Add(arrIdent);
            insertTokens.Add(currentToken.MutateType(TokenType.CLOSE_PAREN));
            insertTokens.Add(currentToken.MutateType(TokenType.OPEN_BRACE));

            //get count
            insertTokens.Add(currentToken.MutateType(TokenType.VAR));
            insertTokens.Add(currentToken.Mutate(TokenType.IDENTIFIER, countIdent));
            insertTokens.Add(currentToken.MutateType(TokenType.ASSIGN));
            insertTokens.Add(currentToken.MutateType(TokenType.COUNT_OF));
            insertTokens.Add(arrIdent);
            insertTokens.Add(currentToken.MutateType(TokenType.END_STATEMENT));

            //if count >0
            insertTokens.Add(currentToken.MutateType(TokenType.IF));
            insertTokens.Add(currentToken.MutateType(TokenType.OPEN_PAREN));
            insertTokens.Add(currentToken.Mutate(TokenType.IDENTIFIER, countIdent));
            insertTokens.Add(currentToken.MutateType(TokenType.GREATER));
            insertTokens.Add(currentToken.Mutate(TokenType.NUMBER, "0.0"));
            insertTokens.Add(currentToken.MutateType(TokenType.CLOSE_PAREN));
            insertTokens.Add(currentToken.MutateType(TokenType.OPEN_BRACE));

            //make i = 0
            insertTokens.Add(currentToken.MutateType(TokenType.VAR));
            insertTokens.Add(currentToken.Mutate(TokenType.IDENTIFIER, iIdent));
            insertTokens.Add(currentToken.MutateType(TokenType.ASSIGN));
            insertTokens.Add(currentToken.Mutate(TokenType.NUMBER, "0.0"));
            insertTokens.Add(currentToken.MutateType(TokenType.END_STATEMENT));

            //make item = arr[0]
            insertTokens.Add(currentToken.MutateType(TokenType.VAR));
            insertTokens.Add(currentToken.Mutate(TokenType.IDENTIFIER, itemIdent));
            insertTokens.Add(currentToken.MutateType(TokenType.ASSIGN));
            insertTokens.Add(arrIdent);
            insertTokens.Add(currentToken.MutateType(TokenType.OPEN_BRACKET));
            insertTokens.Add(currentToken.Mutate(TokenType.IDENTIFIER, iIdent));
            insertTokens.Add(currentToken.MutateType(TokenType.CLOSE_BRACKET));
            insertTokens.Add(currentToken.MutateType(TokenType.END_STATEMENT));

            //make for loop
            insertTokens.Add(currentToken.MutateType(TokenType.FOR));
            insertTokens.Add(currentToken.MutateType(TokenType.OPEN_PAREN));
            insertTokens.Add(currentToken.MutateType(TokenType.END_STATEMENT));
            insertTokens.Add(currentToken.Mutate(TokenType.IDENTIFIER, iIdent));
            insertTokens.Add(currentToken.MutateType(TokenType.LESS));
            insertTokens.Add(currentToken.Mutate(TokenType.IDENTIFIER, countIdent));
            insertTokens.Add(currentToken.MutateType(TokenType.END_STATEMENT));
            insertTokens.Add(currentToken.Mutate(TokenType.IDENTIFIER, iIdent));
            insertTokens.Add(currentToken.MutateType(TokenType.ASSIGN));
            insertTokens.Add(currentToken.Mutate(TokenType.IDENTIFIER, iIdent));
            insertTokens.Add(currentToken.MutateType(TokenType.PLUS));
            insertTokens.Add(currentToken.Mutate(TokenType.NUMBER, "1.0"));
            insertTokens.Add(currentToken.MutateType(TokenType.CLOSE_PAREN));
            insertTokens.Add(currentToken.MutateType(TokenType.OPEN_BRACE));

            //make item = arr[i]
            insertTokens.Add(currentToken.Mutate(TokenType.IDENTIFIER, itemIdent));
            insertTokens.Add(currentToken.MutateType(TokenType.ASSIGN));
            insertTokens.Add(arrIdent);
            insertTokens.Add(currentToken.MutateType(TokenType.OPEN_BRACKET));
            insertTokens.Add(currentToken.Mutate(TokenType.IDENTIFIER, iIdent));
            insertTokens.Add(currentToken.MutateType(TokenType.CLOSE_BRACKET));
            insertTokens.Add(currentToken.MutateType(TokenType.END_STATEMENT));

            //remove the existing 'arr ... {' it's just easier that way
            tokens.RemoveRange(currentTokenIndex, toRemove + 1);

            //find and insert closing } and add another, as we are going to insert 2 {
            var closingBrace = TokenIterator.FindClosing(tokens, currentTokenIndex, TokenType.OPEN_BRACE, TokenType.CLOSE_BRACE);
            tokens.InsertRange(closingBrace, new[] {
                currentToken.MutateType(TokenType.CLOSE_BRACE),
                currentToken.MutateType(TokenType.CLOSE_BRACE),
                currentToken.MutateType(TokenType.CLOSE_BRACE),});

            tokens.InsertRange(currentTokenIndex, insertTokens);
        }

        public void ProcessReplaceEndlessLoop(int currentTokenIndex, List<Token> tokens)
        {
            //we expect `loop {` and we are going to replace with `for(;;)`
            var currentToken = tokens[currentTokenIndex];
            tokens[currentTokenIndex] = currentToken.MutateType(TokenType.FOR);

            tokens.InsertRange(currentTokenIndex + 1, new[] {
                currentToken.MutateType(TokenType.OPEN_PAREN),
                currentToken.MutateType(TokenType.END_STATEMENT),
                currentToken.MutateType(TokenType.END_STATEMENT),
                currentToken.MutateType(TokenType.CLOSE_PAREN),});
        }

        public DesugarStepRequest IsDesugarRequested(TokenIterator tokenIterator, ICompilerDesugarContext context)
        {
            return tokenIterator.CurrentToken.TokenType == TokenType.LOOP
                ? DesugarStepRequest.Replace
                : DesugarStepRequest.None;
        }
    }
}
