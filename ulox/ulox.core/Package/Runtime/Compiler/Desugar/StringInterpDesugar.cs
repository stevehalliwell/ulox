﻿using System.Collections.Generic;

namespace ULox
{
    public class StringInterpDesugar : IDesugarStep
    {
        public DesugarStepRequest IsDesugarRequested(TokenIterator tokenIterator, ICompilerDesugarContext context)
        {
            return tokenIterator.CurrentToken.TokenType == TokenType.STRING
                ? DesugarStepRequest.Replace
                : DesugarStepRequest.None;
        }

        public void ProcessDesugar(int currentTokenIndex, List<Token> tokens, ICompilerDesugarContext context)
        {
            var currentToken = tokens[currentTokenIndex];
            var literalString = currentToken.Literal;
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
                var tokenisedScript = scanner.Scan(new Script("_interp", interpStr));
                interpTokens = tokenisedScript.Tokens;
                interpTokens.RemoveAt(interpTokens.Count - 1);
                interpTokens.InsertRange(0, new[] {
                    currentToken.MutateType(TokenType.PLUS),
                    currentToken.MutateType(TokenType.OPEN_PAREN),});
                interpTokens.AddRange(new[] {
                   currentToken.MutateType(TokenType.CLOSE_PAREN),
                   currentToken.MutateType(TokenType.PLUS),
                   currentToken.Mutate(TokenType.STRING, postInterpStr),});
            }

            newStr = System.Text.RegularExpressions.Regex.Unescape(newStr);
            tokens[currentTokenIndex] = currentToken.Mutate(TokenType.STRING, newStr);

            if (interpTokens != null)
            {
                tokens.InsertRange(currentTokenIndex + 1, interpTokens);
            }
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
