using System.Runtime.CompilerServices;
using System.Text;

namespace ULox
{
    public sealed class DiContainer
    {
        private bool _isFrozen = false;
        private readonly Table _diTable = new Table();

        public int Count => _diTable.Count;

        public void Freeze() 
            => _isFrozen = true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(HashedString name, Value implementation)
            => _diTable[name] = implementation;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(HashedString name, out Value found)
            => _diTable.TryGetValue(name, out found);
    }
}
