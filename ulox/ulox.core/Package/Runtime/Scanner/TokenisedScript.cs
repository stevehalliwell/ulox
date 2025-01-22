using System.Collections.Generic;

namespace ULox
{
    public class TokenisedScript
    {
        public List<Token> Tokens;
        public int[] LineLengths;
        public Script SourceScript;

        public TokenisedScript(List<Token> tokens, int[] lineLengths, Script sourceScript)
        {
            Tokens = tokens;
            LineLengths = lineLengths;
            SourceScript = sourceScript;
        }
    }
}
