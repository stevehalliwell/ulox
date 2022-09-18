namespace ULox
{
    public class CompilerException : UloxException
    {
        public CompilerException(string msg, Token previousToken, string location)
            : base(msg + $" in {location} at {previousToken.Line}:{previousToken.Character}{LiteralStringPartial(previousToken.Literal)}.")
        {
        }

        private static string LiteralStringPartial(object literal)
        {
            if (literal == null) return string.Empty;

            var str = literal.ToString();

            if (string.IsNullOrEmpty(str)) return string.Empty;
            return $" '{str}'";
        }
    }
}
