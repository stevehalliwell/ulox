namespace ULox
{
    public static class CompilerMessageUtil
    {
        public  static string MessageFromContext(string msg, Token previousToken, string location)
        {
            return msg + $" in {location} at {previousToken.Line}:{previousToken.Character}{LiteralStringPartial(previousToken.Literal)}.";
        }

        public  static string MessageFromContext(string msg, Token previousToken, Chunk location)
        {
            return msg + $" in {ChunkToLocationStr(location)} at {previousToken.Line}:{previousToken.Character}{LiteralStringPartial(previousToken.Literal)}.";
        }

        private static string LiteralStringPartial(object literal)
        {
            if (literal == null) return string.Empty;

            var str = literal.ToString();

            if (string.IsNullOrEmpty(str)) return string.Empty;
            return $" '{str}'";
        }

        private static string ChunkToLocationStr(Chunk chunk)
        {
            return $"chunk '{chunk.GetLocationString()}'";
        }
    }

    public class CompilerException : UloxException
    {
        public CompilerException(string msg, Token previousToken, string location)
            : base(CompilerMessageUtil.MessageFromContext(msg, previousToken, location))
        {
        }

        public CompilerException(string msg, Token previousToken, Chunk currentChunk) 
            : base(CompilerMessageUtil.MessageFromContext(msg, previousToken, currentChunk))
        {
        }
    }
}
