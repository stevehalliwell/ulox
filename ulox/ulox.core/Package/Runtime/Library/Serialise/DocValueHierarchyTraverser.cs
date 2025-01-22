using System.Collections.Generic;

namespace ULox
{
    public abstract class DocValueHierarchyTraverser : IDocValueHeirarchyTraverser
    {
        private readonly ValueObjectBuilder _valBuilderRoot;
        protected Stack<ValueObjectBuilder> _builderStack = new();

        protected DocValueHierarchyTraverser(ValueObjectBuilder valBuilder)
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
            => _builderStack.Peek().SetField(name, Value.New(val));
        protected void Field(string name, double val)
            => _builderStack.Peek().SetField(name, Value.New(val));
        protected void Field(string name, bool val)
            => _builderStack.Peek().SetField(name, Value.New(val));

        protected void StartChild(string withName)
            => _builderStack.Push(_builderStack.Peek().CreateChild(withName));

        protected void StartArray(string withName)
            => _builderStack.Push(_builderStack.Peek().CreateArray(withName));

        protected void EndChild() 
            => _builderStack.Pop();
    }
}
