using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace ULox
{
    public sealed class TestRunner
    {
        private readonly Dictionary<string, bool> _testStatus = new();
        private string _lastId;
        private ushort _fixtureLoc;

        public TestRunner(Func<Vm> createVM)
        {
            CreateVM = createVM;
        }

        public bool Enabled { get; set; } = true;
        public bool AllPassed => !_testStatus.Any(x => !x.Value);
        public int TestsFound => _testStatus.Count;
        public HashedString CurrentTestSetName { get; set; } = new HashedString(string.Empty);
        public Func<Vm> CreateVM { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StartTest(Vm vm, string name)
        {
            var id = MakeId(name);
            if (_testStatus.ContainsKey(id))
                vm.ThrowRuntimeException($"{nameof(TestRunner)} found a duplicate test '{id}'");

            _testStatus[id] = false;
            _lastId = id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndTest(string name)
        {
            var id = MakeId(name);
            if (_testStatus.ContainsKey(id))
                _testStatus[id] = true;
            else if (_lastId.StartsWith(id))
                _testStatus[_lastId] = true;

            _lastId = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string MakeId(string name)
        {
            return $"{CurrentTestSetName}:{name}";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GenerateDump()
        {
            var sb = new StringBuilder();

            foreach (var item in _testStatus)
            {
                sb.AppendLine($"{item.Key} {(item.Value ? "Completed" : "Incomplete")}");
            }

            return sb.ToString().Trim();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DoTestOpCode(Vm vm, Chunk chunk, ByteCodePacket.TestOpDetails testOpDetails)
        {
            switch (testOpDetails.TestOpType)
            {
            case TestOpType.TestSetName:
                CurrentTestSetName = chunk.ReadConstant(testOpDetails.b1).val.asString;
                break;
            case TestOpType.TestSetBodyLabel:
                
                _fixtureLoc = chunk.GetLabelPosition(testOpDetails.LabelId);
                break;
            case TestOpType.TestCase:
                if (Enabled)
                {
                    var loc = chunk.GetLabelPosition(testOpDetails.LabelId);
                    RunTestCase(vm, chunk, loc);
                }
                break;
            case TestOpType.CaseStart:
            {
                var b2 = testOpDetails.b2;
                var stringName = chunk.ReadConstant(testOpDetails.b1).val.asString.String; //always string
                if (b2 != 0)
                {
                    stringName += $"({ArgDescriptionString(vm, b2)})";
                }
                StartTest(vm, stringName);
            }
            break;
            case TestOpType.CaseEnd:
            {
                var stringName = chunk.ReadConstant(testOpDetails.b1).val.asString.String; //always string
                EndTest(stringName);
            }
            break;
            case TestOpType.TestSetEnd:
                CurrentTestSetName = new HashedString(string.Empty);
                _fixtureLoc = ushort.MaxValue;
                break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string ArgDescriptionString(Vm vm, byte argCount)
        {
            var sb = new StringBuilder();
            for (int i = argCount - 1; i >= 0; i--)
            {
                sb.Append(vm.Peek(i));
                if (i != 0)
                    sb.Append(", ");
            }
            return sb.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RunTestCase(Vm vm, Chunk chunk, ushort instructionLoc)
        {
            try
            {
                var childVM = CreateVM();
                childVM.CopyFrom(vm);
                childVM.CopyStackFrom(vm);
                childVM.MoveInstructionPointerTo(_fixtureLoc);
                childVM.Run();
                childVM.MoveInstructionPointerTo(instructionLoc);
                childVM.Run();
            }
            catch (UloxException e)
            {
                if (e.Message.StartsWith(nameof(TestRunner)))
                    throw;
                //eat it, results in incomplete test
            }
        }
    }
}
