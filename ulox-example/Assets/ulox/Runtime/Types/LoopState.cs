using System.Collections.Generic;

namespace ULox
{
    public class LoopState
    {
        public int loopContinuePoint = -1;
        public List<int> loopExitPatchLocations = new List<int>();
    }
}
