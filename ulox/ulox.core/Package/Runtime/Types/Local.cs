namespace ULox
{
    public class Local
    {
        public Local(string name, int depth)
        {
            Name = name;
            Depth = depth;
        }

        public string Name { get; private set; }
        public int Depth { get; set; }
        public bool IsCaptured { get; set; }
    }
}
