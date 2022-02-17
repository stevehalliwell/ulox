namespace ULox
{
    public interface ITypeBodyCompilette : ICompilette
    {
        TypeCompiletteStage Stage { get; }

        void Start(TypeCompilette typeCompilette);

        void PreBody(Compiler compiler);

        void PostBody(Compiler compiler);

        void End();
    }
}
