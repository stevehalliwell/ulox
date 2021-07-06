using System.Collections.Generic;

//TODO: Too big, refactor and make more configurable

namespace ULox
{
    public class Compiler : CompilerBase
    {


        protected override void GenerateDeclarationLookup()
        {
            var decl = new List<Compilette>()
            {
                new Compilette(TokenType.TEST, TestDeclaration),
                new Compilette(TokenType.CLASS, ClassDeclaration),
                new Compilette(TokenType.FUNCTION, FunctionDeclaration),
                new Compilette(TokenType.VAR, VarDeclaration),
            };

            foreach (var item in decl)
                declarationCompilettes[item.Match] = item;
        }
    }
}
