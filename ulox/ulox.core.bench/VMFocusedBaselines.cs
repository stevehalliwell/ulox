using BenchmarkDotNet.Attributes;

namespace ULox.Core.Bench
{
    [MemoryDiagnoser]
    [BenchmarkCategory("ALL", "LITE")]
    public class VMFocusedBaselines
    {
        private const int Repeat = 1000;

        private Value _arithmeticLocals;
        private Value _functionCalls;
        private Value _propertyGetSet;
        private Value _methodInvoke;
        private Value _listIndexOps;
        private Value _mapIndexOps;
        private Value _globalAccess;

        private Engine _engine;

        [IterationSetup]
        public void Setup()
        {
            _engine = Program.CreateEngine();
            Load(nameof(ArithmeticLocals), $@"
var sink = 0;
fun {nameof(ArithmeticLocals)}()
{{
    var x = 1;
    var y = 2;
    var total = 0;
    for(var i = 0; i < {Repeat}; i += 1)
    {{
        x = x + 1;
        y = y * 1.001;
        total += x + y;
    }}
    sink = total;
}}
", out _arithmeticLocals);

            Load(nameof(FunctionCalls), $@"
var sink = 0;
fun Add(a, b) {{ retval = a + b; }}
fun {nameof(FunctionCalls)}()
{{
    var total = 0;
    for(var i = 0; i < {Repeat}; i += 1)
    {{
        total = Add(total, i);
    }}
    sink = total;
}}
", out _functionCalls);

            Load(nameof(PropertyGetSet), $@"
class Point {{ var x = 0, y = 0; }}
var sink = 0;
fun {nameof(PropertyGetSet)}()
{{
    var point = Point();
    for(var i = 0; i < {Repeat}; i += 1)
    {{
        point.x = i;
        point.y = point.x + 1;
    }}
    sink = point.y;
}}
", out _propertyGetSet);

            Load(nameof(MethodInvoke), $@"
class Counter
{{
    var x = 0;
    Inc(v)
    {{
        this.x += v;
        retval = this.x;
    }}
}}
var sink = 0;
fun {nameof(MethodInvoke)}()
{{
    var counter = Counter();
    for(var i = 0; i < {Repeat}; i += 1)
    {{
        sink = counter.Inc(i);
    }}
}}
", out _methodInvoke);

            Load(nameof(ListIndexOps), $@"
var sink = 0;
fun {nameof(ListIndexOps)}()
{{
    var arr = [];
    arr.Resize({Repeat}, 0);
    var total = 0;
    for(var i = 0; i < {Repeat}; i += 1)
    {{
        arr[i] = i;
        total += arr[i];
    }}
    sink = total;
}}
", out _listIndexOps);

            Load(nameof(MapIndexOps), $@"
var sink = 0;
fun {nameof(MapIndexOps)}()
{{
    var map = Map();
    var total = 0;
    for(var i = 0; i < {Repeat}; i += 1)
    {{
        map[i] = i;
        total += map[i];
    }}
    sink = total;
}}
", out _mapIndexOps);

            Load(nameof(GlobalAccess), $@"
var globalValue = 0;
fun {nameof(GlobalAccess)}()
{{
    for(var i = 0; i < {Repeat}; i += 1)
    {{
        globalValue += i;
    }}
}}
", out _globalAccess);
        }

        [Benchmark]
        public InterpreterResult ArithmeticLocals() => Run(_arithmeticLocals);

        [Benchmark]
        public InterpreterResult FunctionCalls() => Run(_functionCalls);

        [Benchmark]
        public InterpreterResult PropertyGetSet() => Run(_propertyGetSet);

        [Benchmark]
        public InterpreterResult MethodInvoke() => Run(_methodInvoke);

        [Benchmark]
        public InterpreterResult ListIndexOps() => Run(_listIndexOps);

        [Benchmark]
        public InterpreterResult MapIndexOps() => Run(_mapIndexOps);

        [Benchmark]
        public InterpreterResult GlobalAccess() => Run(_globalAccess);

        private void Load(string name, string source, out Value function)
        {
            _engine.RunScript(new Script(name, source));
            _engine.Context.Vm.Globals.Get(new HashedString(name), out function);
        }

        private InterpreterResult Run(Value function) => _engine.Context.Vm.PushCallFrameAndRun(function, 0);
    }
}
