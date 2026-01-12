using System.Collections.Generic;
using System.Linq;

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

            var expToPreserve = tokens.GetRange(currentTokenIndex + 1, endArrExpAt - currentTokenIndex - 1);

            //remove the existing 'arr ... {' it's just easier that way
            tokens.RemoveRange(currentTokenIndex, toRemove + 1);

            //find and insert closing } and add another, as we are going to insert 2 {
            var closingBrace = TokenIterator.FindClosing(tokens, currentTokenIndex, TokenType.OPEN_BRACE, TokenType.CLOSE_BRACE);
            tokens.InsertRange(closingBrace, new[] {
                currentToken.MutateType(TokenType.CLOSE_BRACE),
                currentToken.MutateType(TokenType.CLOSE_BRACE),
                currentToken.MutateType(TokenType.CLOSE_BRACE),});

            tokens.InsertRange(currentTokenIndex, new[] {
                //make local name for arr exp
                currentToken.MutateType(TokenType.OPEN_BRACE),
                currentToken.MutateType(TokenType.VAR),
                arrIdent,
                currentToken.MutateType(TokenType.ASSIGN) }
            .Concat(expToPreserve)
            .Concat(new[] {
                currentToken.MutateType(TokenType.END_STATEMENT),

                //if arr is valid
                currentToken.MutateType(TokenType.IF),
                currentToken.MutateType(TokenType.OPEN_PAREN),
                arrIdent,
                currentToken.MutateType(TokenType.CLOSE_PAREN),
                currentToken.MutateType(TokenType.OPEN_BRACE),


                //get count
                currentToken.MutateType(TokenType.VAR),
                currentToken.Mutate(TokenType.IDENTIFIER, countIdent),
                currentToken.MutateType(TokenType.ASSIGN),
                currentToken.MutateType(TokenType.COUNT_OF),
                arrIdent,
                currentToken.MutateType(TokenType.END_STATEMENT),

                //if count >0
                currentToken.MutateType(TokenType.IF),
                currentToken.MutateType(TokenType.OPEN_PAREN),
                currentToken.Mutate(TokenType.IDENTIFIER, countIdent),
                currentToken.MutateType(TokenType.GREATER),
                currentToken.Mutate(TokenType.NUMBER, "0.0"),
                currentToken.MutateType(TokenType.CLOSE_PAREN),
                currentToken.MutateType(TokenType.OPEN_BRACE),

                //make i = 0
                currentToken.MutateType(TokenType.VAR),
                currentToken.Mutate(TokenType.IDENTIFIER, iIdent),
                currentToken.MutateType(TokenType.ASSIGN),
                currentToken.Mutate(TokenType.NUMBER, "0.0"),
                currentToken.MutateType(TokenType.END_STATEMENT),

                //make item = arr[0]
                currentToken.MutateType(TokenType.VAR),
                currentToken.Mutate(TokenType.IDENTIFIER, itemIdent),
                currentToken.MutateType(TokenType.ASSIGN),
                arrIdent,
                currentToken.MutateType(TokenType.OPEN_BRACKET),
                currentToken.Mutate(TokenType.IDENTIFIER, iIdent),
                currentToken.MutateType(TokenType.CLOSE_BRACKET),
                currentToken.MutateType(TokenType.END_STATEMENT),

                //make for loop
                currentToken.MutateType(TokenType.FOR),
                currentToken.MutateType(TokenType.OPEN_PAREN),
                //empty init, we did it already
                currentToken.MutateType(TokenType.END_STATEMENT),

                currentToken.Mutate(TokenType.IDENTIFIER, iIdent),
                currentToken.MutateType(TokenType.LESS),
                currentToken.Mutate(TokenType.IDENTIFIER, countIdent),
                currentToken.MutateType(TokenType.END_STATEMENT),

                currentToken.Mutate(TokenType.IDENTIFIER, iIdent),
                currentToken.MutateType(TokenType.ASSIGN),
                currentToken.Mutate(TokenType.IDENTIFIER, iIdent),
                currentToken.MutateType(TokenType.PLUS),
                currentToken.Mutate(TokenType.NUMBER, "1.0"),
                currentToken.MutateType(TokenType.CLOSE_PAREN),

                currentToken.MutateType(TokenType.OPEN_BRACE),
                
                //make item = arr[i]
                currentToken.Mutate(TokenType.IDENTIFIER, itemIdent),
                currentToken.MutateType(TokenType.ASSIGN),
                arrIdent,
                currentToken.MutateType(TokenType.OPEN_BRACKET),
                currentToken.Mutate(TokenType.IDENTIFIER, iIdent),
                currentToken.MutateType(TokenType.CLOSE_BRACKET),
                currentToken.MutateType(TokenType.END_STATEMENT),
                }));
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
