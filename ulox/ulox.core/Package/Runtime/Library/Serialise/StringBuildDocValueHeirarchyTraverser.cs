using System;
using System.Collections.Generic;
using System.IO;

namespace ULox
{
    public class StringBuildDocValueHeirarchyTraverser : DocValueHeirarchyTraverser
    {
        private class StringReaderConsumer
        {
            private StringReader _reader;
            public string CurrentLine { get; private set; }
            public int CurrentIndent { get; private set; }
            public string[] CurrentSplits { get; private set; }
            private bool _consumed = true;

            public StringReaderConsumer(StringReader reader)
            {
                _reader = reader;
            }

            public bool Read()
            {
                if (_consumed)
                {
                    CurrentLine = _reader.ReadLine();

                    if (CurrentLine == null)
                        return false;

                    var numLeadingSpaces = CurrentLine.TrimStart(' ');
                    var leadingSpaces = CurrentLine.Length - numLeadingSpaces.Length;
                    CurrentIndent = leadingSpaces != 0 ? leadingSpaces / 2 : 0;

                    var trimmed = numLeadingSpaces.Trim();
                    CurrentSplits = trimmed.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

                    _consumed = false;
                }

                return CurrentLine != null;
            }

            public void Consume()
            {
                _consumed = true;
            }
        }

        private readonly StringReaderConsumer _consumer;
        private readonly Stack<int> _indentStack = new Stack<int>();

        public StringBuildDocValueHeirarchyTraverser(
            IValueObjectBuilder valBuilder,
            StringReader reader)
            : base (valBuilder)
        {
            _consumer = new StringReaderConsumer(reader);
            _indentStack.Push(0);
        }

        public override void Prepare()
        {
            _consumer.Read();
            _consumer.Consume();
        }

        protected override void ProcessNode()
        {
            while (_consumer.Read())
            {
                if (_consumer.CurrentIndent <= _indentStack.Peek())
                {
                    _indentStack.Pop();
                    EndChild();
                    return;
                }

                if (_consumer.CurrentSplits.Length == 1)
                {
                    if (_consumer.CurrentIndent == _indentStack.Peek() + 1)
                    {
                        _consumer.Consume();
                        _indentStack.Push(_consumer.CurrentIndent);
                        StartChild(_consumer.CurrentSplits[0]);
                        ProcessNode();
                    }
                    else
                        throw new Exception();

                }
                else if (_consumer.CurrentSplits.Length == 2)
                {
                    Field(_consumer.CurrentSplits[0], _consumer.CurrentSplits[1]);
                    _consumer.Consume();
                }
            }
        }
    }
}
