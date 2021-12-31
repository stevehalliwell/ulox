namespace ULox
{
    public abstract class EmptyTypeBodyCompilette : ITypeBodyCompilette
    {
        public abstract TokenType Match { get; }
        public abstract TypeCompiletteStage Stage { get; }

        public void End()
        {
        }

        public void PostBody(CompilerBase compiler)
        {
        }

        public void PreBody(CompilerBase compiler)
        {
        }

        public abstract void Process(CompilerBase compiler);

        public void Start()
        {
        }
    }
}
