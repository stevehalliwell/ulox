using System.Collections.Generic;

namespace ULox
{
    public abstract class DocValueHeirarchyTraverser : IDocValueHeirarchyTraverser
    {
        private readonly IValueObjectBuilder _valBuilderRoot;
        protected Stack<IValueObjectBuilder> _builderStack = new Stack<IValueObjectBuilder>();

        protected DocValueHeirarchyTraverser(IValueObjectBuilder valBuilder)
        {
            _valBuilderRoot = valBuilder;
            _builderStack.Push(_valBuilderRoot);
        }

        public void Process()
        {
            Prepare();
            ProcessNode();
        }

        public abstract void Prepare();
        protected abstract void ProcessNode();

        public Value Finish() 
            => _valBuilderRoot.Finish();

        protected void Field(string name, string val)
            => _builderStack.Peek().SetField(name, val);

        protected void Element(string val)
            => _builderStack.Peek().SetField(null, val);

        protected void StartChild(string withName)
            => _builderStack.Push(_builderStack.Peek().CreateChild(withName));

        protected void StartArray(string withName)
            => _builderStack.Push(_builderStack.Peek().CreateArray(withName));

        protected void EndChild() 
            => _builderStack.Pop();
    }
}
