namespace ULox
{
    public interface ITypeBodyCompilette : ICompilette
    {
        TypeCompiletteStage Stage { get; }

        void Start(TypeCompilette typeCompilette);

        void PostBody(Compiler compiler);
    }
}
