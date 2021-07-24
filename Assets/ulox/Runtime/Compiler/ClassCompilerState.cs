namespace ULox
{
    public class ClassCompilerState
    {
        public string currentClassName;
        public int initFragStartLocation = -1;
        public int previousInitFragJumpLocation = -1;
        public int testFragStartLocation = -1;
        public int previousTestFragJumpLocation = -1;
        //TODO track all the vars we have encountered

        public ClassCompilerState(string currentClassName)
        {
            this.currentClassName = currentClassName;
        }
    }
}
