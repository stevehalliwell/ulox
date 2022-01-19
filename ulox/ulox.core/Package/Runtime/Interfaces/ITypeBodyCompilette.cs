namespace ULox
{
    public interface ITypeBodyCompilette : ICompilette
    {
        TypeCompiletteStage Stage { get; }

        void Start();

        void PreBody(Compiler compiler);

        void PostBody(Compiler compiler);

        void End();
    }
}
