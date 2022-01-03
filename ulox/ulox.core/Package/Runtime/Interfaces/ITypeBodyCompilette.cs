namespace ULox
{
    public interface ITypeBodyCompilette : ICompilette
    {
        TypeCompiletteStage Stage { get; }

        void Start();

        void PreBody(CompilerBase compiler);

        void PostBody(CompilerBase compiler);

        void End();
    }
}
