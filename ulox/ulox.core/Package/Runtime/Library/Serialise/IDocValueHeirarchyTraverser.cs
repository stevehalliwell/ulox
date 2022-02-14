namespace ULox
{
    public interface IDocValueHeirarchyTraverser
    {
        void Process();
        Value Finish();
    }
}