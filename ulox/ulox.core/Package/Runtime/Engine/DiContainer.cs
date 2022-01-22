using System.Text;

namespace ULox
{
    public class DiContainer
    {
        private bool _isFrozen = false;
        private readonly Table _diTable = new Table();

        public int Count => _diTable.Count;

        public void Freeze() 
            => _isFrozen = true;

        public DiContainer ShallowCopy()
        {
            var ret = new DiContainer();
            foreach (var pair in _diTable)
            {
                ret._diTable.Add(pair.Key, pair.Value);
            }

            if (_isFrozen)
                ret.Freeze();

            return ret;
        }

        public string GenerateDump()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Registered in DI{(_isFrozen ? "(frozen)" : "")}:");

            foreach (var item in _diTable)
            {
                sb.AppendLine($"{item.Key}:{item.Value}");
            }

            return sb.ToString().Trim();
        }

        public void Set(HashedString name, Value implementation)
            => _diTable[name] = implementation;

        public bool TryGetValue(HashedString name, out Value found)
            => _diTable.TryGetValue(name, out found);
    }
}
