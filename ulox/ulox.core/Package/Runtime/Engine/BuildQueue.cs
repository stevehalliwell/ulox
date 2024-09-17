using System.Collections.Generic;

namespace ULox
{
    public sealed class BuildQueue
    {
        public readonly List<Script> _buildQueue = new();
        private int _cursor;

        public bool HasItems => _buildQueue.Count > 0;

        public void Enqueue(Script script)
        {
            _buildQueue.Insert(_cursor,script);
            _cursor++;
        }

        public Script Dequeue()
        {
            var ret = _buildQueue[0];
            _buildQueue.RemoveAt(0);
            _cursor = 0;
            return ret;
        }
    }
}
