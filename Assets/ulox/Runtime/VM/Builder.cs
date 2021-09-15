using System;

namespace ULox
{
    public class Builder
    {
        private IEngine _engine;

        public void BindLibrary(string libName)
        {
            _engine.Context.BindLibrary(libName);
        }

        public void LocateScriptAndQueue(string name)
        {
            _engine.LocateAndQueue(name);
        }

        public void SetEngine(IEngine engine) => _engine = engine;
    }
}
