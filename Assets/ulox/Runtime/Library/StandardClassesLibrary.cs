namespace ULox
{
    public class StandardClassesLibrary : IULoxLibrary
    {
        public Table GetBindings()
        {
            var resTable = new Table();
            resTable.Add("List", Value.New(new ListClass()));
            resTable.Add("Map", Value.New(new MapClass()));
            resTable.Add("Dynamic", Value.New(new DynamicClass()));
            return resTable;
        }
    }
}
