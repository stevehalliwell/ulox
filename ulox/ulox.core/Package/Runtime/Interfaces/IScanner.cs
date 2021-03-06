using System.Collections.Generic;

namespace ULox
{
    public interface IScanner
    {
        char CurrentChar { get; }
        int Line { get; set; }
        int CharacterNumber { get; set; }

        void AddGenerator(IScannerTokenGenerator gen);
        void AddToken(TokenType simpleToken, string str, object literal);
        void AddTokenSingle(TokenType token);
        void Advance();
        bool IsAtEnd();
        bool Match(char matchingCharToConsume);
        char Peek();
        void ReadLine();
        void Reset();
        List<Token> Scan(string text);
    }
}
