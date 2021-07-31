using System.Text;

namespace ULox
{
    public class DiContainer
    {
        private bool _isFrozen = false;
        private Table _diTable = new Table();

        public int Count => _diTable.Count;

        public void Freeze() => _isFrozen = true;

        internal DiContainer ShallowCopy()
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

        internal void ReplaceWith(DiContainer diContainerToRestore)
        {
            if (_isFrozen) return;
            _diTable.Clear(); 
            foreach (var pair in diContainerToRestore._diTable)
            {
                _diTable.Add(pair.Key, pair.Value);
            }
        }

        internal string GenerateDump()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Registered in DI{(_isFrozen?"(frozen)":"")}:");

            foreach (var item in _diTable)
            {
                sb.AppendLine($"{item.Key}:{item.Value}");
            }

            return sb.ToString().Trim();
        }

        internal void Set(string name, Value implementation)
            => _diTable[name] = implementation;

        internal bool TryGetValue(string name, out Value found)
            => _diTable.TryGetValue(name, out found);
    }
}
