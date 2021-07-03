namespace ULox
{
    public class StandardClassesLibrary : ILoxByteCodeLibrary
    {
        public Table GetBindings()
        {
            var resTable = new Table();
            resTable.Add("List", Value.New(new ListClass()));
            return resTable;
        }
    }
}
