namespace ULox
{
    public abstract class EmptyTypeBodyCompilette : ITypeBodyCompilette
    {
        public abstract TokenType Match { get; }
        public abstract TypeCompiletteStage Stage { get; }

        public void Start(TypeCompilette typeCompilette)
        {
        }

        public void PreBody(Compiler compiler)
        {
        }

        public abstract void Process(Compiler compiler);

        public void PostBody(Compiler compiler)
        {
        }

        public void End()
        {
        }
    }
}
