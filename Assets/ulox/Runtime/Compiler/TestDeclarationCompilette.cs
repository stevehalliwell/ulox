namespace ULox
{
    public class TestDeclarationCompilette : ICompilette
    {
        private readonly ClassCompilette _classCompilette;

        public TestDeclarationCompilette(ClassCompilette classCompilette)
        {
            _classCompilette = classCompilette;
        }

        public TokenType Match => TokenType.TEST;

        public string CurrentTestSetName { get; internal set; }

        public void Process(CompilerBase compiler)
        {
            TestDeclaration(compiler);
        }

        private void TestDeclaration(CompilerBase compiler)
        {
            //grab name
            var testClassName = (string)compiler.CurrentToken.Literal;
            CurrentTestSetName = testClassName;
            compiler.CurrentChunk.AddConstant(Value.New(testClassName));

            //find the class by name, need to note this instruction so we can patch the argID as it doesn't exist yet
            //var argID = ResolveLocal(compilerStates.Peek(), testClassName);
            //create instance
            //

            //parse as class, class needs to add calls for all testcases it finds to the testFuncChunk
            _classCompilette.Process(compiler);

            compiler.EmitOpCode(OpCode.NULL);
            compiler.EmitOpAndByte(OpCode.ASSIGN_GLOBAL, compiler.CurrentChunk.AddConstant(Value.New(testClassName)));

            CurrentTestSetName = null;
        }
    }
}
