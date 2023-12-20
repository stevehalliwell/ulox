using System.Collections.Generic;

namespace ULox
{
    public class StringInterpDesugar : IDesugarStep
    {
        public DesugarStepRequest RequestFromState(TokenIterator tokenIterator)
        {
            return tokenIterator.CurrentToken.TokenType == TokenType.STRING
                ? DesugarStepRequest.Replace
                : DesugarStepRequest.None;
        }

        public Token ProcessReplace(Token currentToken, int currentTokenIndex, List<Token> tokens)
        {
            var literalString = currentToken.Literal as string;
            var newStr = literalString;
            var locStart = InterpolationStart(literalString, 0);
            var interpTokens = default(List<Token>);
            if (locStart != -1)
            {
                var locEnd = InterpolationEnd(literalString, locStart + 1);
                newStr = literalString.Substring(0, locStart);

                var interpStr = literalString.Substring(locStart + 1, locEnd - locStart - 1);
                var postInterpStr = literalString.Substring(locEnd + 1);
                var scanner = new Scanner();
                interpTokens = scanner.Scan(new Script("_interp", interpStr));
                interpTokens.RemoveAt(interpTokens.Count - 1);
                interpTokens.InsertRange(0, new[] {
                    currentToken.MutateType(TokenType.PLUS),
                    currentToken.MutateType(TokenType.OPEN_PAREN),});
                interpTokens.AddRange(new[] {
                   currentToken.MutateType(TokenType.CLOSE_PAREN),
                   currentToken.MutateType(TokenType.PLUS),
                   currentToken.Mutate(TokenType.STRING,postInterpStr,postInterpStr),});
            }

            newStr = System.Text.RegularExpressions.Regex.Unescape(newStr);
            currentToken = currentToken.Mutate(TokenType.STRING, newStr, newStr);

            if (interpTokens != null)
            {
                tokens.InsertRange(currentTokenIndex + 1, interpTokens);
            }
            return currentToken;
        }

        private static int InterpolationStart(string literalString, int startAt)
        {
            var loc = literalString.IndexOf('{', startAt);
            if (loc == -1)
                return -1;

            if (loc == 0)
                return loc;

            var prevChar = literalString[loc - 1];
            if (prevChar == '\\')
                return InterpolationStart(literalString, loc + 1);

            return loc;
        }

        private static int InterpolationEnd(string literalString, int loc)
        {
            var end = literalString.Length;
            var requiredClose = 1;
            while (loc < end)
            {
                var ch = literalString[loc];
                if (ch == '{')
                    requiredClose++;
                else if (ch == '}')
                    requiredClose--;

                if (requiredClose <= 0)
                    return loc;

                loc++;
            }

            return loc;
        }
    }
}
