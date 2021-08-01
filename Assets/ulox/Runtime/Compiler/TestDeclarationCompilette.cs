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

            //parse as class, class needs to add calls for all testcases it finds to the testFuncChunk
            _classCompilette.Process(compiler);

            //TODO could test set start with name and count
            //TODO could emit list of instructions to run one for each tests

            compiler.EmitOpCode(OpCode.NULL);
            compiler.EmitOpAndBytes(OpCode.ASSIGN_GLOBAL, compiler.CurrentChunk.AddConstant(Value.New(testClassName)));

            CurrentTestSetName = null;
        }
    }
}
