namespace ULox
{
    public class DiLibrary : IULoxLibrary
    {
        private DiContainer _diCont;

        public DiLibrary(DiContainer cont)
        {
            _diCont = cont;
        }

        public string Name => nameof(DiLibrary);

        public Table GetBindings()
        {
            var resTable = new Table();
            var assertInst = new InstanceInternal();
            resTable.Add("DI", Value.New(assertInst));

            assertInst.fields[nameof(Count)] = Value.New(Count);
            assertInst.fields[nameof(GenerateDump)] = Value.New(GenerateDump);

            return resTable;
        }

        private Value Count(VMBase vm, int argCount)
        {
            return Value.New(_diCont.Count);
        }

        private Value GenerateDump(VMBase vm, int argCount)
        {
            return Value.New(_diCont.GenerateDump());
        }
    }
}
